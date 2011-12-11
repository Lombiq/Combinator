using System;
using System.Collections.Generic;
using Autofac.Features.Metadata;
using Orchard;
using Orchard.ContentManagement; // For generic ContentManager methods
using Orchard.DisplayManagement.Descriptors;
using Orchard.DisplayManagement.Descriptors.ResourceBindingStrategy;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.Settings;
using Orchard.Themes;
using Orchard.UI.Resources;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
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
        private readonly IWorkContextAccessor _workContextAccessor;

        public ILogger Logger { get; set; }

        public bool IsDisabled { get; set; }

        public CombinedResourceManager(
            IEnumerable<Meta<IResourceManifestProvider>> resourceProviders,
            ISiteService siteService,
            ICombinatorService combinatorService,
            IShapeTableLocator shapeTableLocator,
            IThemeManager themeManager,
            IWorkContextAccessor workContextAccessor
            )
            : base(resourceProviders)
        {
            _siteService = siteService;
            _combinatorService = combinatorService;
            _shapeTableLocator = shapeTableLocator;
            _themeManager = themeManager;
            _workContextAccessor = workContextAccessor;

            Logger = NullLogger.Instance;
        }

        public override IList<ResourceRequiredContext> BuildRequiredResources(string stringResourceType)
        {
            // It's necessary to make a copy since making a change to the local variable also changes the private one.
            var resources = new List<ResourceRequiredContext>(base.BuildRequiredResources(stringResourceType));

            if (resources.Count == 0 || IsDisabled) return resources;

            var resourceType = ResourceTypeHelper.StringTypeToEnum(stringResourceType);
            var settings = _siteService.GetSiteSettings().As<CombinatorSettingsPart>();

            try
            {
                if (resourceType == ResourceType.Style)
                {
                    // Checking for overridden stylesheets
                    var currentTheme = _themeManager.GetRequestTheme(_workContextAccessor.GetContext().HttpContext.Request.RequestContext);
                    var shapeTable = _shapeTableLocator.Lookup(currentTheme.Id);

                    foreach (var resource in resources)
                    {
                        var shapeName = StylesheetBindingStrategy.GetAlternateShapeNameFromFileName(resource.Resource.GetFullPath());

                        // Simply included CDN stylesheets are not in the ShapeTable, so we have to check
                        if (shapeTable.Bindings.ContainsKey("Style__" + shapeName))
                        {
                            var binding = shapeTable.Bindings["Style__" + shapeName].BindingSource;
                            resource.Resource.SetUrl(binding, null);
                        }
                    }

                    return _combinatorService.CombineStylesheets(resources, settings);
                }
                else if (resourceType == ResourceType.JavaScript)
                {
                    return _combinatorService.CombineScripts(resources, settings);
                }

                return base.BuildRequiredResources(stringResourceType);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error when combining " + resourceType + " files");
                return base.BuildRequiredResources(stringResourceType);
            }
        }
    }
}