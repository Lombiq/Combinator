using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Autofac.Features.Metadata;
using Orchard;
using Orchard.ContentManagement; // For generic ContentManager methods
using Orchard.Environment.Extensions;
using Orchard.Logging;
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
        private readonly IOrchardServices _orchardServices;
        private readonly ICombinatorService _combinatorService;

        public ILogger Logger { get; set; }

        public bool IsDisabled { get; set; }

        public CombinedResourceManager(
            IEnumerable<Meta<IResourceManifestProvider>> resourceProviders,
            IOrchardServices orchardServices,
            ICombinatorService combinatorService)
            : base(resourceProviders)
        {
            _orchardServices = orchardServices;
            combinatorService.ResourceManager = this;
            _combinatorService = combinatorService;

            Logger = NullLogger.Instance;
        }

        public override IList<ResourceRequiredContext> BuildRequiredResources(string stringResourceType)
        {
            var originalResources = base.BuildRequiredResources(stringResourceType);
            if (originalResources.Count == 0 || IsDisabled) return originalResources;


            // Check if the resources fetched were already written to the output or not. If yes, we'll find them here.
            // This is mandatory since resources can be overridden and this is the way to find out which ones were.
            // See also the loop below.
            if (WrittenResources.Count == 0) return originalResources;

            // It's necessary to make a copy since making a change to the local variable also changes the private one. This is most likely some issue
            // with a reference that shouldn't be given away.
            var resources = new List<ResourceRequiredContext>(originalResources);
            ResourceDefinition writtenResource;
            foreach (var resource in resources)
            {
                WrittenResources.TryGetValue(new Tuple<string, string>(stringResourceType, resource.Resource.Name), out writtenResource);
                if (writtenResource == null) return originalResources; // A resource was not yet written...
                resource.Resource.SetUrl(writtenResource.Url);
            }

            var resourceType = ResourceTypeHelper.StringTypeToEnum(stringResourceType);
            var settings = _orchardServices.WorkContext.CurrentSite.As<CombinatorSettingsPart>();


            try
            {
                if (resourceType == ResourceType.Style)
                {
                    return _combinatorService.CombineStylesheets(resources, settings.CombineCDNResources, settings.MinifyResources, settings.MinificationExcludeRegex);
                }
                else if (resourceType == ResourceType.JavaScript)
                {
                    return _combinatorService.CombineScripts(resources, settings.CombineCDNResources, settings.MinifyResources, settings.MinificationExcludeRegex);
                }

                return base.BuildRequiredResources(stringResourceType);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error when combining " + resourceType + " files");
                return base.BuildRequiredResources(stringResourceType);
            }
        }
    }
}