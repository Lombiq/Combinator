using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autofac.Features.Metadata;
using Orchard.Caching;
using Orchard.ContentManagement; // For generic ContentManager methods
using Orchard.DisplayManagement.Descriptors;
using Orchard.DisplayManagement.Descriptors.ResourceBindingStrategy;
using Orchard.Environment.Extensions;
using Orchard.Exceptions;
using Orchard.Logging;
using Orchard.Mvc;
using Orchard.Settings;
using Orchard.Themes;
using Orchard.UI.Resources;
using Piedone.Combinator.EventHandlers;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;
using System.Linq;
using Piedone.HelpfulLibraries.Utilities;
using System.IO;

namespace Piedone.Combinator
{
    /// <summary>
    /// A derivation of the ResourceManager that combines multiple resource files into one, thus speeding up the website download
    /// </summary>
    [OrchardSuppressDependency("Orchard.UI.Resources.ResourceManager")]
    [OrchardFeature("Piedone.Combinator")]
    public class CombinedResourceManager : ResourceManager
    {
        private readonly ISiteService _siteService;
        private readonly ICombinatorService _combinatorService;
        private readonly IShapeTableLocator _shapeTableLocator;
        private readonly IThemeManager _themeManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ILogger Logger { get; set; }


        public CombinedResourceManager(
            IEnumerable<Meta<IResourceManifestProvider>> resourceProviders,
            ISiteService siteService,
            ICombinatorService combinatorService,
            IShapeTableLocator shapeTableLocator,
            IThemeManager themeManager,
            IHttpContextAccessor httpContextAccessor)
            : base(resourceProviders)
        {
            _siteService = siteService;
            _combinatorService = combinatorService;
            _shapeTableLocator = shapeTableLocator;
            _themeManager = themeManager;
            _httpContextAccessor = httpContextAccessor;

            Logger = NullLogger.Instance;
        }


        public override IList<ResourceRequiredContext> BuildRequiredResources(string stringResourceType)
        {
            // It's necessary to make a copy since making a change to the local variable also changes the private one.
            var resources = new List<ResourceRequiredContext>(base.BuildRequiredResources(stringResourceType));

            var settingsPart = _siteService.GetSiteSettings().As<CombinatorSettingsPart>();

            if (resources.Count == 0
                || Orchard.UI.Admin.AdminFilter.IsApplied(_httpContextAccessor.Current().Request.RequestContext) && !settingsPart.EnableForAdmin) return resources;

            var resourceType = ResourceTypeHelper.StringTypeToEnum(stringResourceType);

            try
            {
                Uri resourceBaseUri = null;
                if (!string.IsNullOrEmpty(settingsPart.ResourceBaseUrl)) resourceBaseUri = UriHelper.CreateUri(settingsPart.ResourceBaseUrl);

                var settings = new CombinatorSettings
                {
                    CombineCdnResources = settingsPart.CombineCdnResources,
                    ResourceBaseUri = resourceBaseUri,
                    EmbedCssImages = settingsPart.EmbedCssImages,
                    EmbeddedImagesMaxSizeKB = settingsPart.EmbeddedImagesMaxSizeKB,
                    GenerateImageSprites = settingsPart.GenerateImageSprites,
                    MinifyResources = settingsPart.MinifyResources,
                    EnableResourceSharing = settingsPart.EnableResourceSharing
                };

                if (!string.IsNullOrEmpty(settingsPart.CombinationExcludeRegex)) settings.CombinationExcludeFilter = new Regex(settingsPart.CombinationExcludeRegex);
                if (!string.IsNullOrEmpty(settingsPart.RemoteStorageUrlRegex)) settings.RemoteStorageUrlPattern = new Regex(settingsPart.RemoteStorageUrlRegex);
                if (!string.IsNullOrEmpty(settingsPart.EmbedCssImagesStylesheetExcludeRegex)) settings.EmbedCssImagesStylesheetExcludeFilter = new Regex(settingsPart.EmbedCssImagesStylesheetExcludeRegex);
                if (!string.IsNullOrEmpty(settingsPart.MinificationExcludeRegex)) settings.MinificationExcludeFilter = new Regex(settingsPart.MinificationExcludeRegex);
                if (!string.IsNullOrEmpty(settingsPart.ResourceSharingExcludeRegex)) settings.ResourceSharingExcludeFilter = new Regex(settingsPart.ResourceSharingExcludeRegex);

                if (!string.IsNullOrEmpty(settingsPart.ResourceSetRegexes))
                {
                    var setRegexes = new List<Regex>();
                    foreach (var regex in settingsPart.ResourceSetRegexesEnumerable)
                    {
                        if (!string.IsNullOrEmpty(regex)) setRegexes.Add(new Regex(regex));
                    }
                    settings.ResourceSetFilters = setRegexes.ToArray();
                }

                DiscoverResourceOverrides(resources, resourceType);

                IList<ResourceRequiredContext> result = null;

                for (int retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        if (resourceType == ResourceType.Style) result = _combinatorService.CombineStylesheets(resources, settings);
                        else if (resourceType == ResourceType.JavaScript) result = _combinatorService.CombineScripts(resources, settings);
                        else return base.BuildRequiredResources(stringResourceType);

                        break;
                    }
                    catch (Exception ex)
                    {
                        if (retry < 2)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        else
                        {
                            throw ex;
                        }
                    }
                }

                RemoveOriginalResourceShapes(result, resourceType);

                return result;
            }
            catch (Exception ex)
            {
                if (ex.IsFatal()) throw;
                Logger.Error(ex, "Error when combining " + resourceType + " files");
                return base.BuildRequiredResources(stringResourceType);
            }
        }


        /// <summary>
        /// Checks for overrides of static resources that can cause the actually used resource to be different from the included one
        /// </summary>
        private void DiscoverResourceOverrides(IList<ResourceRequiredContext> resources, ResourceType resourceType)
        {
            TraverseResouceShapes(
                resources,
                resourceType,
                (shapeTable, resource, shapeKey) =>
                {
                    var binding = shapeTable.Bindings[shapeKey].BindingSource;
                    resource.Resource.SetUrl(binding, null);
                });
        }

        /// <summary>
        /// Removes shapes corresponding to resources that are included in their original form (i.e. not combined but excluded). This is
        /// necessary because otherwise the ordering of a collection of original and combined resources are not kept (original resources are
        /// written to the output before the other ones).
        /// </summary>
        private void RemoveOriginalResourceShapes(IList<ResourceRequiredContext> resources, ResourceType resourceType)
        {
            TraverseResouceShapes(
                resources,
                resourceType,
                (shapeTable, resource, shapeKey) =>
                {
                    if (shapeKey.EndsWith("-Combined")) return;

                    // We remove the original shape binding but also re-add it under a different name. The latter operation is needed
                    // because this way shape override detection (e.g. whether a script is overridden in a child theme) can work.
                    var binding = shapeTable.Bindings[shapeKey];
                    shapeTable.Bindings.Remove(shapeKey);
                    shapeTable.Bindings.Add(CombinedShapeKey(shapeKey), binding);
                });
        }

        private void TraverseResouceShapes(IList<ResourceRequiredContext> resources, ResourceType resourceType, Action<ShapeTable, ResourceRequiredContext, string> processor)
        {
            var shapeKeyPrefix = resourceType == ResourceType.Style ? "Style__" : "Script__";

            var currentTheme = _themeManager.GetRequestTheme(_httpContextAccessor.Current().Request.RequestContext);
            var shapeTable = _shapeTableLocator.Lookup(currentTheme.Id);

            foreach (var resource in resources)
            {
                var fullPath = resource.Resource.GetFullPath();
                if (!string.IsNullOrEmpty(fullPath))
                {
                    var shapeName = StaticFileBindingStrategy.GetAlternateShapeNameFromFileName(fullPath);
                    var shapeKey = shapeKeyPrefix + shapeName;

                    // Simply included CDN stylesheets are not in the ShapeTable, so we have to check
                    if (shapeTable.Bindings.ContainsKey(shapeKey))
                    {
                        processor(shapeTable, resource, shapeKey);
                    }
                    else
                    {
                        // Maybe the original binding was removed previously and the combined shape binding remains.
                        shapeKey = CombinedShapeKey(shapeKey);
                        if (shapeTable.Bindings.ContainsKey(shapeKey))
                        {
                            processor(shapeTable, resource, shapeKey);
                        }
                    }
                }
            }
        }

        private static string CombinedShapeKey(string originalShapeKey)
        {
            return originalShapeKey + "-Combined";
        }
    }
}