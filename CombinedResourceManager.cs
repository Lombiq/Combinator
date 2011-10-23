using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private Dictionary<int, IList<ResourceRequiredContext>> _combinedResources = new Dictionary<int, IList<ResourceRequiredContext>>();
        private IList<ResourceRequiredContext> _stylesheetResources = new List<ResourceRequiredContext>();

        private readonly IOrchardServices _orchardServices;
        private readonly ICacheFileService _cacheFileService;
        private readonly IResourceFileService _resourceFileService;

        public ILogger Logger { get; set; }

        public CombinedResourceManager(
            IEnumerable<Meta<IResourceManifestProvider>> resourceProviders,
            IOrchardServices orchardServices,
            ICacheFileService cacheFileService,
            IResourceFileService resourceFileService)
            : base(resourceProviders)
        {
            _orchardServices = orchardServices;
            _cacheFileService = cacheFileService;
            _resourceFileService = resourceFileService;

            Logger = NullLogger.Instance;
        }

        // Possible future code, see http://orchard.codeplex.com/discussions/276210
        public void CombineStylesheets(List<string> urls)
        {
            _stylesheetResources = MakeResourcesFromPublicUrlsWithCDNCombination(urls, ResourceType.Style);
            //var hashCode = resources.GetResourceListHashCode();
            //if (!_cacheFileService.Exists(hashCode))
            //{
            //    var settings = _orchardServices.WorkContext.CurrentSite.As<CombinatorSettingsPart>();
            //    _combinedResources[hashCode] = Combine(resources, hashCode, ResourceType.Style, false);
            //}
        }

        public override IList<ResourceRequiredContext> BuildRequiredResources(string stringResourceType)
        {
            // It's necessary to make a copy since making a change to the local variable also changes the private one. This is most likely some bug
            // with a reference that shouldn't be given away.
            var resources = new List<ResourceRequiredContext>(base.BuildRequiredResources(stringResourceType));
            resources.DeleteCombinedResources();
            if (resources.Count == 0) return resources;

            // Ugly hack to prevent combination of admin resources till the issue is solved
            var rawUrl = _orchardServices.WorkContext.HttpContext.Request.RawUrl;
            if (rawUrl.Contains("/Admin") || rawUrl.Contains("/Packaging/Gallery")) return resources;

            #region Soon-to-be legacy code
            // See http://orchard.codeplex.com/discussions/276210
            var distinctResources = new Dictionary<string, ResourceRequiredContext>(resources.Count); // Overshooting the size
            foreach (var resource in resources)
            {
                var fullPath = resource.Resource.GetFullPath();
                if (!resource.Resource.IsCDNResource())
                {
                    distinctResources[VirtualPathUtility.GetFileName(fullPath)] = resource;
                }
                else
                {
                    distinctResources[fullPath] = resource;
                }
            }
            resources = (from r in distinctResources select r.Value).ToList();
            #endregion

            var hashCode = resources.GetResourceListHashCode();
            var settings = _orchardServices.WorkContext.CurrentSite.As<CombinatorSettingsPart>();
            var resourceType = ResourceTypeHelper.StringTypeToEnum(stringResourceType);

            try
            {
                if (resourceType == ResourceType.Style)
                {
                    if (!_combinedResources.ContainsKey(hashCode))
                    {
                        if (!_cacheFileService.Exists(hashCode))
                        {
                            _combinedResources[hashCode] = Combine(resources, hashCode, resourceType, settings.CombineCDNResources);
                        }
                        else
                        {
                            _combinedResources[hashCode] = MakeResourcesFromPublicUrls(_cacheFileService.GetUrls(hashCode), resources, resourceType, settings.CombineCDNResources);
                        }
                    }

                    return _combinedResources[hashCode];
                }
                else if (resourceType == ResourceType.JavaScript)
                {
                    var combinedScripts = new List<ResourceRequiredContext>(2);

                    Action<ResourceLocation> combineScriptsAtLocation =
                        (location) =>
                        {
                            var locationHashCode = hashCode + (int)location;
                            if (!_combinedResources.ContainsKey(locationHashCode))
                            {
                                var scripts = (from r in resources
                                               where r.Settings.Location == location
                                               select r).ToList();

                                if (!_cacheFileService.Exists(locationHashCode))
                                {
                                    //if (scripts.Count != 0)
                                    _combinedResources[locationHashCode] = Combine(scripts, locationHashCode, resourceType, settings.CombineCDNResources);
                                }
                                else
                                {
                                    _combinedResources[locationHashCode] = MakeResourcesFromPublicUrls(_cacheFileService.GetUrls(locationHashCode), scripts, resourceType, settings.CombineCDNResources);
                                }
                                _combinedResources[locationHashCode].SetLocation(location);
                            }
                            combinedScripts = combinedScripts.Union(_combinedResources[locationHashCode]).ToList();
                        };

                    combineScriptsAtLocation(ResourceLocation.Head);
                    combineScriptsAtLocation(ResourceLocation.Foot);

                    return combinedScripts;
                }

                return base.BuildRequiredResources(stringResourceType);
            }
            catch (Exception e)
            {
                // There was some problem with reading a file
                Logger.Error(e, "Error when combining " + resourceType + " files");
                return base.BuildRequiredResources(stringResourceType);
            }
        }

        /// <summary>
        /// Combines the content of resources
        /// </summary>
        /// <param name="resources">Resources to combine</param>
        /// <param name="hashCode">Just so it shouldn't be recalculated</param>
        /// <param name="type">Type of the resources</param>
        /// <returns>Most of the times the single combined content, but can return more if some of them couldn't be
        /// combined (e.g. was not found or is not a local resource)</returns>
        private IList<ResourceRequiredContext> Combine(IList<ResourceRequiredContext> resources, int hashCode, ResourceType type, bool combineCDNResources)
        {
            var baseUri = new Uri(_orchardServices.WorkContext.CurrentSite.BaseUrl, UriKind.Absolute);
            var combinedContent = new StringBuilder(resources.Count * 1000);

            #region Functions
            Action<string, int> saveCombination =
                (content, insertIndex) =>
                {
                    if (combinedContent.Length == 0) return;

                    var combined = MakeResourceFromPublicUrl(
                            _cacheFileService.Save(hashCode, type, content),
                            type
                        );

                    if (insertIndex == -1) resources.Add(combined);
                    else resources.Insert(insertIndex, combined);

                    combinedContent.Clear();
                };
            #endregion

            var fullPath = "";
            for (int i = 0; i < resources.Count; i++)
            {
                try
                {
                    // Ensuring the resource hasn't got some conditions
                    if (String.IsNullOrEmpty(resources[i].Settings.Condition))
                    {
                        fullPath = resources[i].Resource.GetFullPath();
                        // Ensuring the resource is a local one
                        if (!resources[i].Resource.IsCDNResource())
                        {
                            if (fullPath.StartsWith(baseUri.AbsolutePath))
                            {
                                // Strips e.g. /OrchardLocal
                                if (baseUri.AbsolutePath != "/")
                                {
                                    int Place = fullPath.IndexOf(baseUri.AbsolutePath);
                                    // Finds the first occurence and replaces it with empty string
                                    fullPath = fullPath.Remove(Place, baseUri.AbsolutePath.Length).Insert(Place, ""); 
                                }

                                fullPath = "~" + fullPath;
                            }

                            combinedContent.Append(_resourceFileService.GetLocalResourceContent(fullPath));
                            resources.RemoveAt(i);
                            i--;
                        }
                        else if (combineCDNResources)
                        {
                            _resourceFileService.GetRemoteResourceContent(fullPath);
                            resources.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            // This is to ensure that if there's a remote resource inside a list of local resources, their order stays
                            // the same (so the product is: localResourcesCombined1, remoteResource, localResourceCombined2...)
                            saveCombination(combinedContent.ToString(), i);
                        }
                    }
                    else
                    {
                        // This is to ensure that if there's a conditional resource inside a list of resources, their order stays
                        // the same (so the product is: resourcesCombined1, conditionalResource, resourcesCombined2...)
                        saveCombination(combinedContent.ToString(), i);
                    }
                }
                catch (Exception e)
                {
                    var message = "Downloading of resource " + fullPath + " failed";
                    Logger.Error(e, message);
                    throw new ApplicationException(message, e);
                    //save(combinedContent, i);
                }
            }


            saveCombination(combinedContent.ToString(), -1);

            return resources;
        }

        private IList<ResourceRequiredContext> MakeResourcesFromPublicUrls(IList<string> urls, IList<ResourceRequiredContext> resources, ResourceType type, bool combineCDNResources)
        {
            if (!combineCDNResources) return MakeResourcesFromPublicUrls(urls, resources, type);
            return MakeResourcesFromPublicUrlsWithCDNCombination(urls, type);
        }

        private IList<ResourceRequiredContext> MakeResourcesFromPublicUrlsWithCDNCombination(IList<string> urls, ResourceType type)
        {
            var resources = new List<ResourceRequiredContext>(urls.Count);

            foreach (var url in urls)
            {
                resources.Add(MakeResourceFromPublicUrl(url, type));
            }

            return resources;
        }

        private IList<ResourceRequiredContext> MakeResourcesFromPublicUrls(IList<string> urls, IList<ResourceRequiredContext> resources, ResourceType type)
        {
            List<ResourceRequiredContext> combinedResources;

            combinedResources = new List<ResourceRequiredContext>(resources);
            var urlIndex = 0;
            for (int i = 0; i < combinedResources.Count; i++)
            {
                if (!combinedResources[i].Resource.IsCDNResource())
                {
                    // Overwriting the first local resource with the combined resource
                    combinedResources[i] = MakeResourceFromPublicUrl(urls[urlIndex++], type);
                    // Deleting the other ones to the next remote resource
                    i++;
                    while (i < combinedResources.Count && !combinedResources[i].Resource.IsCDNResource())
                    {
                        combinedResources.RemoveAt(i);
                    }
                }
            }

            return combinedResources;
        }

        private ResourceRequiredContext MakeResourceFromPublicUrl(string url, ResourceType type)
        {
            var resource = new ResourceRequiredContext();

            resource.Settings = Include(ResourceTypeHelper.EnumToStringType(type), url, url);
            resource.Resource = FindResource(resource.Settings);

            return resource;
        }
    }
}