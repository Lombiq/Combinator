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
using Orchard.Mvc;
using Orchard.UI.Resources;
using Piedone.Combinator.EventHandlers;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Models;
using Piedone.HelpfulLibraries.Utilities;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorService : ICombinatorService
    {
        private readonly ICacheFileService _cacheFileService;
        private readonly IResourceProcessingService _resourceProcessingService;
        private readonly ICombinatorResourceManager _combinatorResourceManager;
        private readonly IHttpContextAccessor _hca;

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }


        public CombinatorService(
            ICacheFileService cacheFileService,
            IResourceProcessingService resourceProcessingService,
            ICombinatorResourceManager combinatorResourceManager,
            IHttpContextAccessor hca)
        {
            _cacheFileService = cacheFileService;
            _resourceProcessingService = resourceProcessingService;
            _combinatorResourceManager = combinatorResourceManager;
            _hca = hca;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }


        public IList<ResourceRequiredContext> CombineStylesheets(
            IList<ResourceRequiredContext> resources,
            ICombinatorSettings settings)
        {
            var fingerprint = resources.GetResourceListFingerprint(settings);

            if (!_cacheFileService.Exists(fingerprint, settings))
            {
                Combine(resources, fingerprint, ResourceType.Style, settings);
            }

            return ProcessCombinedResources(_cacheFileService.GetCombinedResources(fingerprint, settings), settings.ResourceBaseUri);
        }

        public IList<ResourceRequiredContext> CombineScripts(
            IList<ResourceRequiredContext> resources,
            ICombinatorSettings settings)
        {
            var combinedScripts = new List<ResourceRequiredContext>(2);

            Action<ResourceLocation> combineScriptsAtLocation =
                (location) =>
                {
                    Predicate<RequireSettings> locationFilter;

                    // Making sure that scripts with unspecified location are treated equally to foot scripts. Theoretically
                    // this isn't right (it can change what "unspecified" results in) but it deals with the lazyness of developers.
                    if (location == ResourceLocation.Head)
                    {
                        locationFilter = s => s.Location == ResourceLocation.Head;
                    }
                    else
                    {
                        locationFilter = s => s.Location == ResourceLocation.Foot || s.Location == ResourceLocation.Unspecified;
                    }

                    var scripts = (from r in resources
                                   where locationFilter(r.Settings)
                                   select r).ToList();

                    var fingerprint = scripts.GetResourceListFingerprint(settings);

                    IList<ResourceRequiredContext> combinedResourcesAtLocation;

                    if (!scripts.Any()) combinedResourcesAtLocation = new List<ResourceRequiredContext>();
                    else
                    {
                        if (!_cacheFileService.Exists(fingerprint, settings))
                        {
                            Combine(scripts, fingerprint, ResourceType.JavaScript, settings);
                        }

                        combinedResourcesAtLocation = ProcessCombinedResources(_cacheFileService.GetCombinedResources(fingerprint, settings), settings.ResourceBaseUri);
                        combinedResourcesAtLocation.SetLocation(location);
                    }


                    combinedScripts = combinedScripts.Union(combinedResourcesAtLocation).ToList();
                };

            combineScriptsAtLocation(ResourceLocation.Head);
            combineScriptsAtLocation(ResourceLocation.Foot);

            return combinedScripts;
        }


        /// <summary>
        /// Combines (and minifies) the content of resources and saves the combinations
        /// </summary>
        /// <param name="resources">Resources to combine</param>
        /// <param name="fingerprint">Just so it shouldn't be recalculated</param>
        /// <param name="resourceType">Type of the resources</param>
        /// <param name="settings">Combination settings</param>
        /// <exception cref="ApplicationException">Thrown if there was a problem with a resource file (e.g. it was missing or could not be opened)</exception>
        private void Combine(IList<ResourceRequiredContext> resources, string fingerprint, ResourceType resourceType, ICombinatorSettings settings)
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
                    resource.Settings.Attributes,
                    resource.Resource.TagBuilder.Attributes);

                combinatorResource.IsRemoteStorageResource = settings.RemoteStorageUrlPattern != null && settings.RemoteStorageUrlPattern.IsMatch(combinatorResource.AbsoluteUrl.ToString());

                combinatorResources.Add(combinatorResource);
            }

            var combinedContent = new StringBuilder(1000);
            var resourceBaseUri = settings.ResourceBaseUri != null ? settings.ResourceBaseUri : new Uri(_hca.Current().Request.Url, "/");

            Action<CombinatorResource, List<CombinatorResource>> saveCombination =
                (combinedResource, containedResources) =>
                {
                    if (combinedResource == null) return;

                    // Don't save emtpy resources
                    if (combinedContent.Length == 0 && !combinedResource.IsOriginal) return;

                    if (!containedResources.Any()) containedResources = new List<CombinatorResource> { combinedResource };

                    var bundleFingerprint = containedResources.GetCombinatorResourceListFingerprint(settings);

                    var localSettings = new CombinatorSettings(settings);
                    //var useResourceSharing = settings.EnableResourceSharing;
                    if (localSettings.EnableResourceSharing && settings.ResourceSharingExcludeFilter != null)
                    {
                        foreach (var resource in containedResources)
                        {
                            if (localSettings.EnableResourceSharing)
                            {
                                localSettings.EnableResourceSharing = !settings.ResourceSharingExcludeFilter.IsMatch(resource.AbsoluteUrl.ToString());
                            }
                        }
                    }

                    if (!combinedResource.IsOriginal)
                    {
                        combinedResource.Content = combinedContent.ToString();

                        if (combinedResource.Type == ResourceType.Style && !string.IsNullOrEmpty(combinedResource.Content) && settings.GenerateImageSprites)
                        {
                            _resourceProcessingService.ReplaceCssImagesWithSprite(combinedResource);
                        }

                        combinedResource.Content =
                            "/*" + Environment.NewLine
                            + "Resource bundle created by Combinator (http://combinator.codeplex.com/)" + Environment.NewLine + Environment.NewLine
                            + "Resources in this bundle:" + Environment.NewLine
                            + string.Join(Environment.NewLine, containedResources.Select(resource =>
                                {
                                    var url = resource.AbsoluteUrl.ToString();
                                    if (localSettings.EnableResourceSharing && !resource.IsCdnResource && !resource.IsRemoteStorageResource)
                                    {
                                        var uriBuilder = new UriBuilder(url);
                                        uriBuilder.Host = "DefaultTenant";
                                        url = uriBuilder.Uri.ToStringWithoutScheme();
                                    }
                                    return "- " + url;
                                }))
                            + Environment.NewLine + "*/"
                            + Environment.NewLine + Environment.NewLine + Environment.NewLine + combinedResource.Content;
                    }


                    // We save a bundle now. First the bundle should be saved separately under its unique name, then for this resource list.
                    if (bundleFingerprint != fingerprint && containedResources.Count > 1)
                    {
                        if (!_cacheFileService.Exists(bundleFingerprint, localSettings))
                        {
                            _cacheFileService.Save(bundleFingerprint, combinedResource, localSettings);
                        }

                        // Overriding the url for the resource in this resource list with the url of the set.
                        combinedResource.IsOriginal = true;

                        // The following should fetch one result theoretically but can more if the above Exists()-Save() happens
                        // in multiple requests at the same time.
                        var set = _cacheFileService.GetCombinedResources(bundleFingerprint, localSettings).First();

                        combinedResource.LastUpdatedUtc = set.LastUpdatedUtc;

                        if (IsOwnedResource(set))
                        {
                            if (settings.ResourceBaseUri != null && !set.IsRemoteStorageResource)
                            {
                                combinedResource.RequiredContext.Resource.SetUrl(UriHelper.Combine(resourceBaseUri.ToStringWithoutScheme(), set.AbsoluteUrl.PathAndQuery));
                            }
                            else if (set.IsRemoteStorageResource)
                            {
                                combinedResource.IsRemoteStorageResource = true;
                                combinedResource.RequiredContext.Resource.SetUrl(set.AbsoluteUrl.ToStringWithoutScheme());
                            }
                            else
                            {
                                combinedResource.RequiredContext.Resource.SetUrl(set.RelativeUrl.ToString());
                            }

                            AddTimestampToUrl(combinedResource);
                        }
                        else
                        {
                            combinedResource.RequiredContext.Resource.SetUrl(set.AbsoluteUrl.ToStringWithoutScheme());
                        }
                    }

                    _cacheFileService.Save(fingerprint, combinedResource, localSettings);

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
                var saveOriginalResource = false;

                try
                {
                    absoluteUrlString = resource.AbsoluteUrl.ToString();

                    if (settings.CombinationExcludeFilter == null || !settings.CombinationExcludeFilter.IsMatch(absoluteUrlString))
                    {
                        // If this resource differs from the previous one in terms of settings or CDN they can't be combined
                        if (previousResource != null
                            && (!previousResource.SettingsEqual(resource) || (previousResource.IsCdnResource != resource.IsCdnResource && !settings.CombineCdnResources)))
                        {
                            saveCombination(previousResource, resourcesInCombination);
                            previousResource = null; // So it doesn't get combined again in if (saveOriginalResource) below.
                        }

                        // If this resource is in a different set than the previous, they can't be combined
                        if (currentSetRegex != null && !currentSetRegex.IsMatch(absoluteUrlString))
                        {
                            currentSetRegex = null;
                            saveCombination(previousResource, resourcesInCombination);
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
                                saveCombination(previousResource, resourcesInCombination);
                            }
                        }

                        // Nesting resources in such blocks is needed because some syntax is valid if in its own file but not anymore
                        // when bundled.
                        if (resourceType == ResourceType.JavaScript) combinedContent.Append("{");
                        combinedContent.Append(Environment.NewLine);

                        _resourceProcessingService.ProcessResource(resource, combinedContent, settings);

                        // This can be because e.g. it's a CDN resource and CDN combination is disabled.
                        if (resource.IsOriginal) saveOriginalResource = true;

                        combinedContent.Append(Environment.NewLine);
                        if (resourceType == ResourceType.JavaScript) combinedContent.Append("}");
                        combinedContent.Append(Environment.NewLine);

                        resourcesInCombination.Add(resource);
                    }
                    else saveOriginalResource = true;

                    if (saveOriginalResource)
                    {
                        // This is a fully excluded resource
                        if (previousResource != null) saveCombination(previousResource, resourcesInCombination);
                        resource.IsOriginal = true;
                        saveCombination(resource, resourcesInCombination);
                        combinatorResources[i] = null; // So previous resource detection works correctly
                    }
                }
                catch (Exception ex)
                {
                    if (ex.IsFatal()) throw;
                    throw new OrchardException(T("Processing of resource {0} failed.", absoluteUrlString), ex);
                }
            }


            saveCombination(combinatorResources[combinatorResources.Count - 1], resourcesInCombination);
        }


        private static IList<ResourceRequiredContext> ProcessCombinedResources(IEnumerable<CombinatorResource> combinedResources, Uri resourceBaseUri)
        {
            // This is really an inline Distinct(), see: http://stackoverflow.com/a/4158364/220230
            // Making the list distinct is needed so if there are duplicated resources because of simultaneous processing then resources
            // are still not included multiple times.
            combinedResources = combinedResources.GroupBy(resource => resource.AbsoluteUrl).Select(group => group.First());

            IList<ResourceRequiredContext> resources = new List<ResourceRequiredContext>(combinedResources.Count());

            foreach (var resource in combinedResources)
            {
                if (IsOwnedResource(resource))
                {
                    AddTimestampToUrl(resource);
                    if (resourceBaseUri != null)
                    {
                        if (!resource.IsRemoteStorageResource)
                        {
                            resource.RequiredContext.Resource.SetUrl(UriHelper.Combine(resourceBaseUri.ToStringWithoutScheme(), resource.RequiredContext.Resource.Url)); 
                        }
                        else
                        {
                            resource.RequiredContext.Resource.SetUrl(UriHelper.Combine(resourceBaseUri.ToStringWithoutScheme(), resource.AbsoluteUrl.PathAndQuery));
                        }
                    }
                }

                resources.Add(resource.RequiredContext);
            }

            return resources;
        }

        private static void AddTimestampToUrl(CombinatorResource resource)
        {
            var uriBuilder = new UriBuilder(resource.AbsoluteUrl);
            uriBuilder.Query = "timestamp=" + resource.LastUpdatedUtc.ToFileTimeUtc(); // Using UriBuilder for this is maybe an overkill
            var urlString = resource.IsCdnResource || resource.IsRemoteStorageResource ? uriBuilder.Uri.ToStringWithoutScheme() : uriBuilder.Uri.PathAndQuery.ToString();
            resource.RequiredContext.Resource.SetUrl(urlString);
        }

        private static bool IsOwnedResource(CombinatorResource resource)
        {
            return (!resource.IsCdnResource && !resource.IsOriginal) || resource.IsRemoteStorageResource;
        }
    }
}