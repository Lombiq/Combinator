using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Orchard;
using Orchard.Logging;
using Orchard.UI.Resources;
// For generic ContentManager methods
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
using Yahoo.Yui.Compressor;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorService : ICombinatorService
    {
        /// <summary>
        /// Instance cache for combined resources
        /// </summary>
        /// <remarks>
        /// Is reasonable when a Combine*() method is called multiple times in the same request (that's currently only with js files)
        /// </remarks>
        private Dictionary<int, IList<ResourceRequiredContext>> _combinedResources = new Dictionary<int, IList<ResourceRequiredContext>>();

        private readonly IOrchardServices _orchardServices;
        private readonly ICacheFileService _cacheFileService;
        private readonly IResourceFileService _resourceFileService;

        public ILogger Logger { get; set; }

        public static bool IsDisabled { get; set; }
        public IResourceManager ResourceManager { get; set; }

        public CombinatorService(
            IOrchardServices orchardServices,
            ICacheFileService cacheFileService,
            IResourceFileService resourceFileService)
        {
            _orchardServices = orchardServices;
            _cacheFileService = cacheFileService;
            _resourceFileService = resourceFileService;

            Logger = NullLogger.Instance;
        }

        public IList<ResourceRequiredContext> CombineStylesheets(
            IList<ResourceRequiredContext> resources, 
            bool combineCDNResources = false, 
            bool minifyResources = true, 
            string minificationExcludeRegex = "")
        {
            var hashCode = resources.GetResourceListHashCode();

            if (!_combinedResources.ContainsKey(hashCode))
            {
                if (!_cacheFileService.Exists(hashCode))
                {
                    _combinedResources[hashCode] = Combine(resources, hashCode, ResourceType.Style, combineCDNResources, minifyResources, minificationExcludeRegex);
                }
                else
                {
                    _combinedResources[hashCode] = MakeResourcesFromPublicUrls(_cacheFileService.GetUrls(hashCode), resources, ResourceType.Style, combineCDNResources);
                }
            }

            return _combinedResources[hashCode];
        }

        public IList<ResourceRequiredContext> CombineScripts(
            IList<ResourceRequiredContext> resources, 
            bool combineCDNResources = false, 
            bool minifyResources = true,
            string minificationExcludeRegex = "")
        {
            var hashCode = resources.GetResourceListHashCode();
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
                            _combinedResources[locationHashCode] = Combine(scripts, locationHashCode, ResourceType.JavaScript, combineCDNResources, minifyResources, minificationExcludeRegex);
                        }
                        else
                        {
                            _combinedResources[locationHashCode] = MakeResourcesFromPublicUrls(_cacheFileService.GetUrls(locationHashCode), scripts, ResourceType.JavaScript, combineCDNResources);
                        }
                        _combinedResources[locationHashCode].SetLocation(location);
                    }
                    combinedScripts = combinedScripts.Union(_combinedResources[locationHashCode]).ToList();
                };

            combineScriptsAtLocation(ResourceLocation.Head);
            combineScriptsAtLocation(ResourceLocation.Foot);

            return combinedScripts;
        }

        /// <summary>
        /// Combines the content of resources
        /// </summary>
        /// <param name="resources">Resources to combine</param>
        /// <param name="hashCode">Just so it shouldn't be recalculated</param>
        /// <param name="resourceType">Type of the resources</param>
        /// <param name="combineCDNResources">Whether CDN resources should be combined or not</param>
        /// <param name="minifyResources">If true, resources will be minified</param>
        /// <param name="minificationExcludeRegex">The regex to use when excluding resources from minification</param>
        /// <returns>Most of the times the single combined content, but can return more if some of them couldn't be
        /// combined (e.g. was not found or is not a local resource)</returns>
        /// <exception cref="ApplicationException">Thrown if there was a problem with a resource file (i.e. it was missing or could not be opened)</exception>
        private IList<ResourceRequiredContext> Combine(IList<ResourceRequiredContext> resources, int hashCode, ResourceType resourceType, bool combineCDNResources, bool minifyResources, string minificationExcludeRegex)
        {
            var combinedContent = new StringBuilder(resources.Count * 1000);

            #region Functions
            Action<string, int> saveCombination =
                (content, insertIndex) =>
                {
                    if (combinedContent.Length == 0) return;

                    var combined = MakeResourceFromPublicUrl(
                            _cacheFileService.Save(hashCode, resourceType, content),
                            resourceType
                        );

                    if (insertIndex == -1) resources.Add(combined);
                    else resources.Insert(insertIndex, combined);

                    combinedContent.Clear();
                };
            #endregion

            var applicationPath = _orchardServices.WorkContext.HttpContext.Request.ApplicationPath;
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
                            if (fullPath.StartsWith(applicationPath))
                            {
                                // Strips e.g. /OrchardLocal
                                if (applicationPath != "/")
                                {
                                    int Place = fullPath.IndexOf(applicationPath);
                                    // Finds the first occurence and replaces it with empty string
                                    fullPath = fullPath.Remove(Place, applicationPath.Length).Insert(Place, "");
                                }

                                fullPath = "~" + fullPath;
                            }

                            var content = _resourceFileService.GetLocalResourceContent(fullPath);

                            // Modify relative paths to have correct values
                            var uriSegments = fullPath.Replace("~", "").Split('/'); // Path class is not good for this
                            var parentDirUrl = String.Join("/", uriSegments.Take(uriSegments.Length - 2).ToArray()) + "/"; // Jumping up a directory
                            content = Regex.Replace(content, "\\.\\./", parentDirUrl, RegexOptions.IgnoreCase);


                            if (resourceType == ResourceType.Style)
                            {
                                var stylesheetDirUrl = String.Join("/", uriSegments.Take(uriSegments.Length - 1).ToArray()) + "/";
                                //content = Regex.Replace(content, "(url\\(['|\"]?)", "$1" + stylesheetDirUrl, RegexOptions.IgnoreCase);
                                content = Regex.Replace(
                                    content,
                                    "url\\(['|\"]?(.+?)['|\"]?\\)",
                                    match =>
                                    {
                                        if (!Uri.IsWellFormedUriString(match.Groups[1].Value, UriKind.Absolute)) // Ensuring it's a local url
                                        {
                                            return "url(\"" + stylesheetDirUrl + match.Groups[1].Value + "\")";
                                        }
                                        return match.Groups[0].Value;
                                    },
                                    RegexOptions.IgnoreCase);
                            }

                            if (minifyResources && (String.IsNullOrEmpty(minificationExcludeRegex) || !Regex.IsMatch(fullPath, minificationExcludeRegex)))
                            {
                                if (resourceType == ResourceType.Style)
                                {
                                    content = CssCompressor.Compress(content);
                                }
                                else if (resourceType == ResourceType.JavaScript)
                                {
                                    content = JavaScriptCompressor.Compress(content);
                                }
                            }

                            combinedContent.Append(content);
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
                    var message = "Processing of resource " + fullPath + " failed";
                    Logger.Error(e, message);
                    throw new ApplicationException(message, e);
                    //save(combinedContent, i);
                }
            }


            saveCombination(combinedContent.ToString(), -1);

            return resources;
        }

        private IList<ResourceRequiredContext> MakeResourcesFromPublicUrls(IList<string> urls, IList<ResourceRequiredContext> resources, ResourceType resourceType, bool combineCDNResources)
        {
            if (ResourceManager == null) throw new ApplicationException("The ResourceManager instance should be set after instantiating.");

            if (!combineCDNResources) return MakeResourcesFromPublicUrls(urls, resources, resourceType);
            return MakeResourcesFromPublicUrlsWithCDNCombination(urls, resourceType);
        }

        private IList<ResourceRequiredContext> MakeResourcesFromPublicUrlsWithCDNCombination(IList<string> urls, ResourceType resourceType)
        {
            var resources = new List<ResourceRequiredContext>(urls.Count);

            foreach (var url in urls)
            {
                resources.Add(MakeResourceFromPublicUrl(url, resourceType));
            }

            return resources;
        }

        private IList<ResourceRequiredContext> MakeResourcesFromPublicUrls(IList<string> urls, IList<ResourceRequiredContext> resources, ResourceType resourceType)
        {
            List<ResourceRequiredContext> combinedResources;

            combinedResources = new List<ResourceRequiredContext>(resources);
            var urlIndex = 0;
            for (int i = 0; i < combinedResources.Count; i++)
            {
                if (!combinedResources[i].Resource.IsCDNResource())
                {
                    // Overwriting the first local resource with the combined resource
                    combinedResources[i] = MakeResourceFromPublicUrl(urls[urlIndex++], resourceType);
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

        private ResourceRequiredContext MakeResourceFromPublicUrl(string url, ResourceType resourceType)
        {
            var resource = new ResourceRequiredContext();

            // This is only necessary to buld the ResourceRequiredContext object, therefore we also delete the resource
            // from the required ones.
            resource.Settings = ResourceManager.Include(ResourceTypeHelper.EnumToStringType(resourceType), url, url);
            resource.Resource = ResourceManager.FindResource(resource.Settings);
            ResourceManager.NotRequired(ResourceTypeHelper.EnumToStringType(resourceType), resource.Resource.Name);

            return resource;
        }
    }
}