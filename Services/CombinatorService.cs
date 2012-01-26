using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Orchard;
using Orchard.Caching;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.UI.Resources;
// For generic ContentManager methods
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
using Piedone.Combinator.Models;
using Piedone.HelpfulLibraries.DependencyInjection;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorService : ICombinatorService
    {
        private readonly ICacheFileService _cacheFileService;
        private readonly IResourceProcessingService _resourceProcessingService;
        private readonly IResolve<ISmartResource> _smartResourceResolve;
        private readonly ICacheManager _cacheManager;

        public ILogger Logger { get; set; }

        public CombinatorService(
            ICacheFileService cacheFileService,
            IResourceProcessingService resourceProcessingService,
            IResolve<ISmartResource> smartResourceResolve,
            ICacheManager cacheManager)
        {
            _cacheFileService = cacheFileService;
            _resourceProcessingService = resourceProcessingService;
            _smartResourceResolve = smartResourceResolve;
            _cacheManager = cacheManager;

            Logger = NullLogger.Instance;
        }

        public IList<ResourceRequiredContext> CombineStylesheets(
            IList<ResourceRequiredContext> resources,
            ICombinatorSettings settings)
        {
            var hashCode = resources.GetResourceListHashCode();

            return _cacheManager.Get("Piedone.Combinator.CombinedResources." + hashCode.ToString(), ctx =>
            {
                if (!_cacheFileService.Exists(hashCode))
                {
                    Combine(resources, hashCode, ResourceType.Style, settings);
                }

                _cacheFileService.MonitorCacheChangedSignal(ctx, hashCode);

                return ProcessCombinedResources(_cacheFileService.GetCombinedResources(hashCode));
            });
        }

        public IList<ResourceRequiredContext> CombineScripts(
            IList<ResourceRequiredContext> resources,
            ICombinatorSettings settings)
        {
            var hashCode = resources.GetResourceListHashCode();
            var combinedScripts = new List<ResourceRequiredContext>(2);

            Action<ResourceLocation> combineScriptsAtLocation =
                (location) =>
                {
                    var locationHashCode = hashCode + (int)location;

                    var combinedResourcesAtLocation = _cacheManager.Get("Piedone.Combinator.CombinedResources." + locationHashCode.ToString(), ctx =>
                    {
                        if (!_cacheFileService.Exists(locationHashCode))
                        {
                            var scripts = (from r in resources
                                           where r.Settings.Location == location
                                           select r).ToList();

                            if (scripts.Count == 0) return new List<ResourceRequiredContext>();

                            Combine(scripts, locationHashCode, ResourceType.JavaScript, settings);
                        }

                        _cacheFileService.MonitorCacheChangedSignal(ctx, locationHashCode);

                        var combinedResources = ProcessCombinedResources(_cacheFileService.GetCombinedResources(locationHashCode));
                        combinedResources.SetLocation(location);

                        return combinedResources;
                    });

                    combinedScripts = combinedScripts.Union(combinedResourcesAtLocation).ToList();
                };

            // Scripts at different locations are processed separately
            // Currently this will add two files at the foot if scripts with unspecified location are also included
            combineScriptsAtLocation(ResourceLocation.Head);
            combineScriptsAtLocation(ResourceLocation.Foot);
            combineScriptsAtLocation(ResourceLocation.Unspecified);

            return combinedScripts;
        }

        /// <summary>
        /// Combines (and minifies) the content of resources and saves the combinations
        /// </summary>
        /// <param name="resources">Resources to combine</param>
        /// <param name="hashCode">Just so it shouldn't be recalculated</param>
        /// <param name="resourceType">Type of the resources</param>
        /// <param name="settings">Combination settings</param>
        /// <exception cref="ApplicationException">Thrown if there was a problem with a resource file (e.g. it was missing or could not be opened)</exception>
        private void Combine(IList<ResourceRequiredContext> resources, int hashCode, ResourceType resourceType, ICombinatorSettings settings)
        {
            if (resources.Count == 0) return;

            var smartResources = new List<ISmartResource>(resources.Count);
            foreach (var resource in resources)
            {
                var smartResource = _smartResourceResolve.Value;
                smartResource.Type = resourceType;
                smartResource.FillRequiredContext(resource); // Copying the context so the original one won't be touched
                smartResources.Add(smartResource);
            }

            var combinedContent = new StringBuilder(1000);

            Action<ISmartResource> saveCombination =
                (combinedResource) =>
                {
                    combinedResource.Content = combinedContent.ToString();
                    combinedResource.Type = resourceType;
                    _cacheFileService.Save(hashCode, combinedResource);

                    combinedContent.Clear();
                };


            for (int i = 0; i < smartResources.Count; i++)
            {
                var resource = smartResources[i];
                var previousResource = (i != 0) ? smartResources[i - 1] : null;
                var publicUrl = "";

                try
                {
                    publicUrl = resource.PublicUrl.ToString();

                    if ((String.IsNullOrEmpty(settings.CombinationExcludeRegex) || !Regex.IsMatch(publicUrl, settings.CombinationExcludeRegex)))
                    {
                        // If this resource differs from the previous one in terms of settings or CDN, they can't be combined
                        if (previousResource != null &&
                            (!previousResource.SettingsEqual(resource) || (previousResource.IsCDNResource != resource.IsCDNResource && !settings.CombineCDNResources)))
                        {
                            saveCombination(previousResource);
                        }

                        _resourceProcessingService.ProcessResource(resource, combinedContent, settings);
                    }
                    else
                    {
                        if (previousResource != null) saveCombination(previousResource);
                        resource.OverrideCombinedUrl(resource.PublicUrl);
                        saveCombination(resource);
                        smartResources[i] = null;
                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("Processing of resource " + publicUrl + " failed", ex);
                }
            }


            saveCombination(smartResources[smartResources.Count - 1]);
        }

        private IList<ResourceRequiredContext> ProcessCombinedResources(IList<ISmartResource> combinedResources)
        {
            IList<ResourceRequiredContext> resources = new List<ResourceRequiredContext>(combinedResources.Count);

            foreach (var resource in combinedResources)
            {
                if (!resource.IsCDNResource)
                {
                    var uriBuilder = new UriBuilder(resource.PublicUrl);
                    uriBuilder.Query = "timestamp=" + resource.LastUpdatedUtc.ToFileTimeUtc(); // Using UriBuilder for this is maybe an overkill
                    resource.RequiredContext.Resource.SetUrl(uriBuilder.Uri.PathAndQuery.ToString()); // Using relative urls
                }
                resources.Add(resource.RequiredContext);
            }

            return resources;
        }
    }
}