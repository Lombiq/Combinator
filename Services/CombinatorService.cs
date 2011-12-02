using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Orchard;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.UI.Resources;
// For generic ContentManager methods
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
using Yahoo.Yui.Compressor;

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

        private readonly ICacheFileService _cacheFileService;
        private readonly IResourceFileService _resourceFileService;

        public ILogger Logger { get; set; }

        public IResourceManager ResourceManager { get; set; }

        public CombinatorService(
            ICacheFileService cacheFileService,
            IResourceFileService resourceFileService)
        {
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
                    _combinedResources[hashCode] = MakeResourcesFromPublicUrls(_cacheFileService.GetPublicUrls(hashCode), resources, ResourceType.Style, combineCDNResources);
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
                            _combinedResources[locationHashCode] = MakeResourcesFromPublicUrls(_cacheFileService.GetPublicUrls(locationHashCode), scripts, ResourceType.JavaScript, combineCDNResources);
                        }
                        _combinedResources[locationHashCode].SetLocation(location);
                    }
                    combinedScripts = combinedScripts.Union(_combinedResources[locationHashCode]).ToList();
                };

            // Scripts at different locations are processed separately
            // Currently this will add two files at the foot if scripts with unspecified location are also included
            combineScriptsAtLocation(ResourceLocation.Head);
            combineScriptsAtLocation(ResourceLocation.Foot);
            combineScriptsAtLocation(ResourceLocation.Unspecified);

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
            var combinedResources = new List<ResourceRequiredContext>();

            #region Functions
            Action saveCombination =
                () =>
                {
                    if (combinedContent.Length == 0) return;

                    combinedResources.Add(
                        MakeResourceFromPublicUrl(
                            _cacheFileService.Save(hashCode, resourceType, combinedContent.ToString()),
                            resourceType
                        )
                    );

                    combinedContent.Clear();
                };

            Func<string, bool> hasToBeMinified =
                (path) =>
                {
                    return minifyResources && (String.IsNullOrEmpty(minificationExcludeRegex) || !Regex.IsMatch(path, minificationExcludeRegex));
                };
            #endregion

            var fullPath = "";
            try
            {
                foreach (var resource in resources)
                {
                    fullPath = resource.Resource.GetFullPath();

                    // Only unconditional resources are combined
                    if (String.IsNullOrEmpty(resource.Settings.Condition))
                    {
                        // Ensuring the resource is a local one
                        if (!resource.Resource.IsCDNResource())
                        {
                            var virtualPath = _resourceFileService.GetRelativeVirtualPath(fullPath);

                            var content = _resourceFileService.GetLocalResourceContent(virtualPath);

                            content = AdjustRelativePaths(content, _resourceFileService.GetPublicRelativeUrl(virtualPath), resourceType);

                            if (hasToBeMinified(fullPath))
                            {
                                content = MinifyResourceContent(content, resourceType);
                            }

                            combinedContent.Append(content);
                        }
                        else if (combineCDNResources)
                        {
                            var content = _resourceFileService.GetRemoteResourceContent(fullPath);

                            if (hasToBeMinified(fullPath))
                            {
                                content = MinifyResourceContent(content, resourceType);
                            }

                            combinedContent.Append(content);
                        }
                        else
                        {
                            // This is to ensure that if there's a remote resource inside a list of local resources, their order stays
                            // the same (so the product is: localResourcesCombined1, remoteResource, localResourceCombined2...)
                            saveCombination();
                            combinedResources.Add(resource);
                        } 
                    }
                    else
                    {
                        // This is to ensure that if there's a conditional resource inside a list of resources, it stays alone
                        saveCombination();
                        
                        // We currently don't minify conditional resources
                        combinedResources.Add(resource);
                    }
                }
            }
            catch (Exception e)
            {
                var message = "Processing of resource " + fullPath + " failed";
                throw new ApplicationException(message, e);
            }

            saveCombination();

            return combinedResources;
        }

        private string AdjustRelativePaths(string content, string publicUrl, ResourceType resourceType)
        {
            // Modify relative paths to have correct values
            var uriSegments = publicUrl.Split('/'); // Path class is not good for this
            var parentDirUrl = String.Join("/", uriSegments.Take(uriSegments.Length - 2).ToArray()) + "/"; // Jumping up a directory
            content = content.Replace("../", parentDirUrl);

            // Modify relative paths that point to the same dir as the stylesheet's to have correct values
            if (resourceType == ResourceType.Style)
            {
                var stylesheetDirUrl = parentDirUrl + uriSegments[uriSegments.Length - 2] + "/";
                content = Regex.Replace(
                                        content,
                                        "url\\(['|\"]?(.+?)['|\"]?\\)",
                                        (match) =>
                                        {
                                            var url = match.Groups[1].ToString();

                                            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute) && !url.StartsWith("../") && !url.StartsWith("/"))
                                            {
                                                url = stylesheetDirUrl + url;
                                            }

                                            return "url(\"" + url + "\")";
                                        },
                                        RegexOptions.IgnoreCase);
            }

            return content;
        }

        private string MinifyResourceContent(string content, ResourceType resourceType)
        {
            if (resourceType == ResourceType.Style)
            {
                return CssCompressor.Compress(content);
            }
            else if (resourceType == ResourceType.JavaScript)
            {
                return JavaScriptCompressor.Compress(content);
            }

            return content;
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

            // This is only necessary to build the ResourceRequiredContext object, therefore we also delete the resource
            // from the required ones.
            resource.Settings = ResourceManager.Include(ResourceTypeHelper.EnumToStringType(resourceType), url, url);
            resource.Resource = ResourceManager.FindResource(resource.Settings);
            ResourceManager.NotRequired(ResourceTypeHelper.EnumToStringType(resourceType), resource.Resource.Name);

            return resource;
        }
    }
}