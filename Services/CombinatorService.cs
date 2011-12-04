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
using Yahoo.Yui.Compressor;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorService : ICombinatorService
    {
        private readonly ICacheFileService _cacheFileService;
        private readonly IResourceFileService _resourceFileService;
        private readonly WorkContext _workContext;
        private readonly ICacheManager _cacheManager;

        public ILogger Logger { get; set; }

        public CombinatorService(
            ICacheFileService cacheFileService,
            IResourceFileService resourceFileService,
            WorkContext workContext,
            ICacheManager cacheManager)
        {
            _cacheFileService = cacheFileService;
            _resourceFileService = resourceFileService;
            _workContext = workContext;
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
                else
                {
                    // Cache invalidation signals will be only monitored if the combined resources were properly saved.
                    // Not doing so would just immediately invalidate this cache entry anyway.
                    _cacheFileService.MonitorCacheChangedSignal(ctx, hashCode);
                }

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
                        else
                        {
                            // Cache invalidation signals will be only monitored if the combined resources were properly saved.
                            // Not doing so would just immediately invalidate this cache entry anyway.
                            _cacheFileService.MonitorCacheChangedSignal(ctx, locationHashCode);
                        }

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
            var combinedContent = new StringBuilder(resources.Count * 1000);

            #region Functions
            Action<ISmartResource> saveCombination =
                (combinedResource) =>
                {
                    combinedResource.Content = combinedContent.ToString();
                    combinedResource.Type = resourceType;
                    _cacheFileService.Save(hashCode, combinedResource);

                    combinedContent.Clear();
                };

            Func<string, bool> hasToBeMinified =
                (path) =>
                {
                    return settings.MinifyResources && (String.IsNullOrEmpty(settings.MinificationExcludeRegex) || !Regex.IsMatch(path, settings.MinificationExcludeRegex));
                };
            #endregion

            var smartResources = new List<ISmartResource>(resources.Count);
            foreach (var resource in resources)
            {
                var smartResource = NewResource();
                smartResource.RequiredContext = resource;
                smartResources.Add(smartResource);
            }

            var fullPath = "";
            ISmartResource previousResource = null;
            try
            {
                foreach (var resource in smartResources)
                {
                    fullPath = resource.FullPath;

                    if (previousResource != null && !previousResource.SerializableSettingsEqual(resource))
                    {
                        saveCombination(previousResource);
                    }

                    if ((String.IsNullOrEmpty(settings.CombinationExcludeRegex) || !Regex.IsMatch(fullPath, settings.CombinationExcludeRegex)))
                    {
                        if (!resource.IsCDNResource)
                        {
                            var virtualPath = resource.RelativeVirtualPath;

                            var content = _resourceFileService.GetLocalResourceContent(virtualPath);

                            content = AdjustRelativePaths(content, resource.PublicRelativeUrl, resourceType);

                            if (hasToBeMinified(fullPath))
                            {
                                content = MinifyResourceContent(content, resourceType);
                            }

                            combinedContent.Append(content);
                        }
                        else if (settings.CombineCDNResources)
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
                            resource.UrlOverride = resource.FullPath;
                        }
                    }
                    else
                    {
                        resource.UrlOverride = resource.FullPath;
                    }

                    previousResource = resource;
                }
            }
            catch (Exception e)
            {
                var message = "Processing of resource " + fullPath + " failed";
                throw new ApplicationException(message, e);
            }

            saveCombination(previousResource);
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

        private IList<ResourceRequiredContext> ProcessCombinedResources(IList<ISmartResource> combinedResources)
        {
            return (from r in combinedResources select r.RequiredContext).ToList();
        }

        private ISmartResource NewResource()
        {
            return _workContext.Resolve<ISmartResource>();
        }
    }
}