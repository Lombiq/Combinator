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
        private readonly ICombinatorEventMonitor _combinatorEventMonitor;

        public ILogger Logger { get; set; }


        public CombinedResourceManager(
            IEnumerable<Meta<IResourceManifestProvider>> resourceProviders,
            ISiteService siteService,
            ICombinatorService combinatorService,
            IShapeTableLocator shapeTableLocator,
            IThemeManager themeManager,
            IHttpContextAccessor httpContextAccessor,
            ICacheManager cacheManager,
            ICombinatorEventMonitor combinatorEventMonitor)
            : base(resourceProviders)
        {
            _siteService = siteService;
            _combinatorService = combinatorService;
            _shapeTableLocator = shapeTableLocator;
            _themeManager = themeManager;
            _httpContextAccessor = httpContextAccessor;
            _combinatorEventMonitor = combinatorEventMonitor;

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
                var settings = new CombinatorSettings
                {
                    CombineCDNResources = settingsPart.CombineCdnResources,
                    ResourceDomain = settingsPart.ResourceDomain,
                    EmbedCssImages = settingsPart.EmbedCssImages,
                    EmbeddedImagesMaxSizeKB = settingsPart.EmbeddedImagesMaxSizeKB,
                    GenerateImageSprites = settingsPart.GenerateImageSprites,
                    MinifyResources = settingsPart.MinifyResources
                };

                if (!String.IsNullOrEmpty(settingsPart.CombinationExcludeRegex)) settings.CombinationExcludeFilter = new Regex(settingsPart.CombinationExcludeRegex);
                if (!String.IsNullOrEmpty(settingsPart.EmbedCssImagesStylesheetExcludeRegex)) settings.EmbedCssImagesStylesheetExcludeFilter = new Regex(settingsPart.EmbedCssImagesStylesheetExcludeRegex);
                if (!String.IsNullOrEmpty(settingsPart.MinificationExcludeRegex)) settings.MinificationExcludeFilter = new Regex(settingsPart.MinificationExcludeRegex);

                if (!String.IsNullOrEmpty(settingsPart.ResourceSetRegexes))
                {
                    var setRegexes = new List<Regex>();
                    foreach (var regex in settingsPart.ResourceSetRegexesEnumerable)
                    {
                        if (!String.IsNullOrEmpty(regex)) setRegexes.Add(new Regex(regex));
                    }
                    settings.ResourceSetFilters = setRegexes.ToArray();
                }

                DiscoverResourceOverrides(resources, resourceType);

                IList<ResourceRequiredContext> result;

                if (resourceType == ResourceType.Style) result = _combinatorService.CombineStylesheets(resources, settings);
                else if (resourceType == ResourceType.JavaScript) result = _combinatorService.CombineScripts(resources, settings);
                else return base.BuildRequiredResources(stringResourceType);

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
                    shapeTable.Bindings.Remove(shapeKey);
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
                if (!String.IsNullOrEmpty(fullPath))
                {
                    var shapeName = StaticFileBindingStrategy.GetAlternateShapeNameFromFileName(fullPath);
                    var shapeKey = shapeKeyPrefix + shapeName;

                    // Simply included CDN stylesheets are not in the ShapeTable, so we have to check
                    if (shapeTable.Bindings.ContainsKey(shapeKey))
                    {
                        processor(shapeTable, resource, shapeKey);
                    }
                }
            }
        }
    }
}