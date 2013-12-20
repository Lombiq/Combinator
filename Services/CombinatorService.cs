using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Orchard;
using Orchard.Caching.Services;
using Orchard.Environment.Extensions;
using Orchard.Exceptions;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.UI.Resources;
using Piedone.Combinator.EventHandlers;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorService : ICombinatorService
    {
        private readonly ICacheFileService _cacheFileService;
        private readonly IResourceProcessingService _resourceProcessingService;
        private readonly ICacheService _cacheService;
        private readonly ICombinatorEventMonitor _combinatorEventMonitor;
        private readonly ICombinatorResourceManager _combinatorResourceManager;

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }


        public CombinatorService(
            ICacheFileService cacheFileService,
            IResourceProcessingService resourceProcessingService,
            ICacheService cacheService,
            ICombinatorEventMonitor combinatorEventMonitor,
            ICombinatorResourceManager combinatorResourceManager)
        {
            _cacheFileService = cacheFileService;
            _resourceProcessingService = resourceProcessingService;
            _cacheService = cacheService;
            _combinatorEventMonitor = combinatorEventMonitor;
            _combinatorResourceManager = combinatorResourceManager;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }


        public IList<ResourceRequiredContext> CombineStylesheets(
            IList<ResourceRequiredContext> resources,
            ICombinatorSettings settings)
        {
            var hashCode = resources.GetResourceListHashCode();
            var cacheKey = MakeCacheKey(hashCode) + ".Styles";

            return _cacheService.Get(cacheKey, () =>
            {
                if (!_cacheFileService.Exists(hashCode))
                {
                    Combine(resources, hashCode, ResourceType.Style, settings);
                }

                _combinatorEventMonitor.MonitorCacheEmptied(cacheKey);

                return ProcessCombinedResources(_cacheFileService.GetCombinedResources(hashCode), settings.ResourceDomain);
            });
        }

        public IList<ResourceRequiredContext> CombineScripts(
            IList<ResourceRequiredContext> resources,
            ICombinatorSettings settings)
        {
            var hashCode = resources.GetResourceListHashCode();
            var combinedScripts = new List<ResourceRequiredContext>(2);

            Func<ResourceLocation, List<ResourceRequiredContext>> filterScripts =
                (location) =>
                {
                    return (from r in resources
                            where r.Settings.Location == location
                            select r).ToList();
                };

            Action<ResourceLocation> combineScriptsAtLocation =
                (location) =>
                {
                    var locationHashCode = hashCode + (int)location;
                    var cacheKey = MakeCacheKey(locationHashCode) + ".Scripts";

                    var combinedResourcesAtLocation = _cacheService.Get(cacheKey, () =>
                    {
                        if (!_cacheFileService.Exists(locationHashCode))
                        {
                            var scripts = filterScripts(location);

                            if (scripts.Count == 0) return new List<ResourceRequiredContext>();

                            Combine(scripts, locationHashCode, ResourceType.JavaScript, settings);
                        }

                        _combinatorEventMonitor.MonitorCacheEmptied(cacheKey);

                        var combinedResources = ProcessCombinedResources(_cacheFileService.GetCombinedResources(locationHashCode), settings.ResourceDomain);
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

            var combinatorResources = new List<CombinatorResource>(resources.Count);
            foreach (var resource in resources)
            {
                var combinatorResource = _combinatorResourceManager.ResourceFactory(resourceType);

                // Copying the context so the original one won't be touched
                combinatorResource.FillRequiredContext(
                    resource.Resource.Name,
                    resource.Resource.GetFullPath(),
                    resource.Settings.Culture,
                    resource.Settings.Condition,
                    resource.Settings.Attributes);

                combinatorResources.Add(combinatorResource);
            }

            var combinedContent = new StringBuilder(1000);

            Action<CombinatorResource, List<CombinatorResource>, int> saveCombination =
                (combinedResource, containedResources, bundleHashCode) =>
                {
                    if (combinedResource == null) return;

                    // Don't save emtpy resources
                    if (combinedContent.Length == 0 && !combinedResource.IsOriginal) return;

                    if (!combinedResource.IsOriginal)
                    {
                        combinedResource.Content = combinedContent.ToString();

                        if (combinedResource.Type == ResourceType.Style && !String.IsNullOrEmpty(combinedResource.Content) && settings.GenerateImageSprites)
                        {
                            _resourceProcessingService.ReplaceCssImagesWithSprite(combinedResource);
                        }

                        combinedResource.Content =
                            "/*" + Environment.NewLine
                            + "Resource bundle created by Combinator (http://combinator.codeplex.com/)" + Environment.NewLine + Environment.NewLine
                            + "Resources in this bundle:" + Environment.NewLine
                            + String.Join(Environment.NewLine, containedResources.Select(resource => "- " + resource.AbsoluteUrl.ToString()).ToArray())
                            + Environment.NewLine + "*/"
                            + Environment.NewLine + Environment.NewLine + Environment.NewLine + combinedResource.Content;
                    }


                    // We save a bundle now. First the bundle should be saved separately under its unique name, then for this resource list.
                    if (bundleHashCode != hashCode)
                    {
                        if (!_cacheFileService.Exists(bundleHashCode))
                        {
                            _cacheFileService.Save(bundleHashCode, combinedResource);
                        }

                        // Overriding the url for the resource in this resource list with the url of the set.
                        combinedResource.IsOriginal = true;
                        var set = _cacheFileService.GetCombinedResources(bundleHashCode).Single(); // Should be one resource
                        combinedResource.RequiredContext.Resource.SetUrl(set.AbsoluteUrl.ToProtocolRelative());
                        combinedResource.LastUpdatedUtc = set.LastUpdatedUtc;
                        AddTimestampToUrl(combinedResource);
                    }

                    _cacheFileService.Save(hashCode, combinedResource);

                    combinedContent.Clear();
                    containedResources.Clear();
                };


            Regex currentSetRegex = null;
            var resourcesInCombination = new List<CombinatorResource>();

            for (int i = 0; i < combinatorResources.Count; i++)
            {
                var resource = combinatorResources[i];
                var previousResource = (i != 0) ? combinatorResources[i - 1] : null;
                var absoluteUrlString = "";

                try
                {
                    absoluteUrlString = resource.AbsoluteUrl.ToString();

                    if (settings.CombinationExcludeFilter == null || !settings.CombinationExcludeFilter.IsMatch(absoluteUrlString))
                    {
                        // If this resource differs from the previous one in terms of settings or CDN they can't be combined
                        if (previousResource != null
                            && (!previousResource.SettingsEqual(resource) || (previousResource.IsCdnResource != resource.IsCdnResource && !settings.CombineCDNResources)))
                        {
                            saveCombination(previousResource, resourcesInCombination, hashCode);
                        }

                        // If this resource is in a different set than the previous, they can't be combined
                        if (currentSetRegex != null && !currentSetRegex.IsMatch(absoluteUrlString))
                        {
                            currentSetRegex = null;
                            saveCombination(previousResource, resourcesInCombination, resourcesInCombination.GetCombinatorResourceListHashCode());
                        }

                        // Calculate if this resource is in a set
                        if (currentSetRegex == null && settings.ResourceSetFilters != null && settings.ResourceSetFilters.Length > 0)
                        {
                            int r = 0;
                            while (currentSetRegex == null && r < settings.ResourceSetFilters.Length)
                            {
                                if (settings.ResourceSetFilters[r].IsMatch(absoluteUrlString)) currentSetRegex = settings.ResourceSetFilters[r];
                                r++;
                            }

                            // The previous resource is in a different set or in no set so it can't be combined with this resource
                            if (currentSetRegex != null && previousResource != null && resourcesInCombination.Any())
                            {
                                saveCombination(previousResource, resourcesInCombination, hashCode);
                            }
                        }

                        // Nesting resources in such blocks is needed because some syntax is valid if in its own file but not anymore
                        // when bundled.
                        if (resourceType == ResourceType.JavaScript) combinedContent.Append("{");
                        combinedContent.Append(Environment.NewLine);

                        _resourceProcessingService.ProcessResource(resource, combinedContent, settings);

                        combinedContent.Append(Environment.NewLine);
                        if (resourceType == ResourceType.JavaScript) combinedContent.Append("}");
                        combinedContent.Append(Environment.NewLine);

                        resourcesInCombination.Add(resource);
                    }
                    else
                    {
                        // This is a fully excluded resource
                        if (previousResource != null) saveCombination(previousResource, resourcesInCombination, hashCode);
                        resource.IsOriginal = true;
                        saveCombination(resource, resourcesInCombination, hashCode);
                        combinatorResources[i] = null; // So previous resource detection works correctly
                    }
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal()) throw;
                    throw new OrchardException(T("Processing of resource {0} failed.", absoluteUrlString), ex);
                }
            }


            saveCombination(combinatorResources[combinatorResources.Count - 1], resourcesInCombination, hashCode);
        }


        private static IList<ResourceRequiredContext> ProcessCombinedResources(IList<CombinatorResource> combinedResources, string resourceDomain)
        {
            IList<ResourceRequiredContext> resources = new List<ResourceRequiredContext>(combinedResources.Count);

            foreach (var resource in combinedResources)
            {
                if ((!resource.IsCdnResource && !resource.IsOriginal) || resource.IsRemoteStorageResource)
                {
                    AddTimestampToUrl(resource);
                    if (!String.IsNullOrEmpty(resourceDomain)) resource.RequiredContext.Resource.SetUrl(resourceDomain + resource.RequiredContext.Resource.Url);
                }

                resources.Add(resource.RequiredContext);
            }

            return resources;
        }

        private static string MakeCacheKey(int hashCode)
        {
            return "Piedone.Combinator.CombinedResources." + hashCode.ToString();
        }

        private static void AddTimestampToUrl(CombinatorResource resource)
        {
            var uriBuilder = new UriBuilder(resource.AbsoluteUrl);
            uriBuilder.Query = "timestamp=" + resource.LastUpdatedUtc.ToFileTimeUtc(); // Using UriBuilder for this is maybe an overkill
            var urlString = resource.IsCdnResource ? uriBuilder.Uri.ToProtocolRelative() : uriBuilder.Uri.PathAndQuery.ToString();
            resource.RequiredContext.Resource.SetUrl(urlString);
        }
    }
}