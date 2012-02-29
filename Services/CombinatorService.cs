using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Orchard.Caching;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.UI.Resources;
// For generic ContentManager methods
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
using Piedone.Combinator.Models;
using Piedone.HelpfulLibraries.DependencyInjection;
using Orchard.TaskLease.Services;
using Orchard.Services;
using System.Threading;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorService : ICombinatorService
    {
        private readonly ICacheFileService _cacheFileService;
        private readonly IResourceProcessingService _resourceProcessingService;
        private readonly IResolve<ISmartResource> _smartResourceResolve;
        private readonly ICacheManager _cacheManager;
        private readonly ITaskLeaseService _taskLeaseService;
        private readonly IClock _clock;

        public ILogger Logger { get; set; }

        public CombinatorService(
            ICacheFileService cacheFileService,
            IResourceProcessingService resourceProcessingService,
            IResolve<ISmartResource> smartResourceResolve,
            ICacheManager cacheManager,
            ITaskLeaseService taskLeaseService,
            IClock clock)
        {
            _cacheFileService = cacheFileService;
            _resourceProcessingService = resourceProcessingService;
            _smartResourceResolve = smartResourceResolve;
            _cacheManager = cacheManager;
            _taskLeaseService = taskLeaseService;
            _clock = clock;

            Logger = NullLogger.Instance;
        }

        public IList<ResourceRequiredContext> CombineStylesheets(
            IList<ResourceRequiredContext> resources,
            ICombinatorSettings settings)
        {
            var hashCode = resources.GetResourceListHashCode();
            var taskName = MakeTaskName(hashCode);

            return _cacheManager.Get(taskName, ctx =>
            {
                if (!_cacheFileService.Exists(hashCode))
                {
                    if (!TryAcquireTask(taskName)) return resources;
                    try
                    {
                        Combine(resources, hashCode, ResourceType.Style, settings);
                    }
                    finally
                    {
                        FreeTask(taskName);
                    }
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

                    var taskName = MakeTaskName(locationHashCode);
                    var combinedResourcesAtLocation = _cacheManager.Get(taskName, ctx =>
                    {
                        if (!_cacheFileService.Exists(locationHashCode))
                        {
                            var scripts = (from r in resources
                                           where r.Settings.Location == location
                                           select r).ToList();

                            if (scripts.Count == 0) return new List<ResourceRequiredContext>();

                            if (!TryAcquireTask(taskName)) return scripts;
                            try
                            {
                                Combine(scripts, locationHashCode, ResourceType.JavaScript, settings);
                            }
                            finally
                            {
                                FreeTask(taskName);
                            }
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
                    // Don't save emtpy resources
                    if (combinedContent.Length == 0 && !combinedResource.CombinedUrlIsOverridden) return;

                    combinedResource.Content = combinedContent.ToString();
                    combinedResource.Type = resourceType;
                    _cacheFileService.Save(hashCode, combinedResource);

                    combinedContent.Clear();
                };


            Regex currentSetRegex = null;

            for (int i = 0; i < smartResources.Count; i++)
            {
                var resource = smartResources[i];
                var previousResource = (i != 0) ? smartResources[i - 1] : null;
                var publicUrlString = "";

                try
                {
                    publicUrlString = resource.PublicUrl.ToString();

                    if (settings.CombinationExcludeFilter == null || !settings.CombinationExcludeFilter.IsMatch(publicUrlString))
                    {
                        // If this resource differs from the previous one in terms of settings or CDN they can't be combined
                        if (previousResource != null
                            && (!previousResource.SettingsEqual(resource) || (previousResource.IsCDNResource != resource.IsCDNResource && !settings.CombineCDNResources)))
                        {
                            saveCombination(previousResource);
                        }

                        // If this resource is in a different set than the previous, they can't be combined
                        if (currentSetRegex != null && !currentSetRegex.IsMatch(publicUrlString))
                        {
                            currentSetRegex = null;
                            saveCombination(previousResource);
                        }

                        // Calculate if this resource is in a set
                        if (currentSetRegex == null && settings.ResourceSetFilters != null && settings.ResourceSetFilters.Length > 0)
                        {
                            int r = 0;
                            while (currentSetRegex == null && r < settings.ResourceSetFilters.Length)
                            {
                                if (settings.ResourceSetFilters[r].IsMatch(publicUrlString)) currentSetRegex = settings.ResourceSetFilters[r];
                                r++;
                            }

                            // The previous resource is in a different set or in no set so it can't be combined with this resource
                            if (currentSetRegex != null && previousResource != null)
                            {
                                saveCombination(previousResource);
                            }
                        }

                        _resourceProcessingService.ProcessResource(resource, combinedContent, settings);
                    }
                    else
                    {
                        if (previousResource != null) saveCombination(previousResource);
                        resource.OverrideCombinedUrl(resource.PublicUrl);
                        saveCombination(resource);
                        smartResources[i] = null; // So previous resource detection works correctly
                    }
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("Processing of resource " + publicUrlString + " failed", ex);
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

        private bool TryAcquireTask(string taskName)
        {
            // Trying to acquire task so no interference with other requests performing the same one happens
            int waitSeconds = 1;
            int maxRunSeconds = 4;
            while (_taskLeaseService.Acquire(taskName, _clock.UtcNow.AddSeconds(maxRunSeconds)) == null && waitSeconds < maxRunSeconds)
            {
                Thread.Sleep(waitSeconds * 1000);
                waitSeconds *= 2;
            }

            return waitSeconds != maxRunSeconds;
        }

        private void FreeTask(string taskName)
        {
            //_taskLeaseService.Update(taskName, "", _clock.UtcNow.Subtract(new TimeSpan(1, 0, 0)));
        }

        private static string MakeTaskName(int hashCode)
        {
            return "Piedone.Combinator.CombinedResources." + hashCode.ToString();
        }
    }
}