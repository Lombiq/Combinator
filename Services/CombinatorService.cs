using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.UI.Resources;
// For generic ContentManager methods
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
using Yahoo.Yui.Compressor;
using Piedone.Combinator.Models;
using Orchard.Environment;
using Orchard;

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
        private readonly WorkContext _workContext;

        public ILogger Logger { get; set; }

        public CombinatorService(
            ICacheFileService cacheFileService,
            IResourceFileService resourceFileService,
            WorkContext workContext)
        {
            _cacheFileService = cacheFileService;
            _resourceFileService = resourceFileService;
            _workContext = workContext;

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
                    Combine(resources, hashCode, ResourceType.Style, combineCDNResources, minifyResources, minificationExcludeRegex);
                }

                _combinedResources[hashCode] = ProcessCombinedResources(_cacheFileService.GetCombinedResources(hashCode));
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

                        if (scripts.Count == 0) return;

                        if (!_cacheFileService.Exists(locationHashCode))
                        {
                            Combine(scripts, locationHashCode, ResourceType.JavaScript, combineCDNResources, minifyResources, minificationExcludeRegex);
                        }

                        _combinedResources[locationHashCode] = ProcessCombinedResources(_cacheFileService.GetCombinedResources(locationHashCode));

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
        /// Combines (and minifies) the content of resources and saves the combinations
        /// </summary>
        /// <param name="resources">Resources to combine</param>
        /// <param name="hashCode">Just so it shouldn't be recalculated</param>
        /// <param name="resourceType">Type of the resources</param>
        /// <param name="combineCDNResources">Whether CDN resources should be combined or not</param>
        /// <param name="minifyResources">If true, resources will be minified</param>
        /// <param name="minificationExcludeRegex">The regex to use when excluding resources from minification</param>
        /// <exception cref="ApplicationException">Thrown if there was a problem with a resource file (e.g. it was missing or could not be opened)</exception>
        private void Combine(IList<ResourceRequiredContext> resources, int hashCode, ResourceType resourceType, bool combineCDNResources, bool minifyResources, string minificationExcludeRegex)
        {
            var combinedContent = new StringBuilder(resources.Count * 1000);

            #region Functions
            Action<ISmartResource> saveCombination =
                (combinedResource) =>
                {
                    if (combinedContent.Length == 0) return;

                    combinedResource.Content = combinedContent.ToString();
                    combinedResource.Type = resourceType;
                    _cacheFileService.Save(hashCode, combinedResource);

                    combinedContent.Clear();
                };

            Func<string, bool> hasToBeMinified =
                (path) =>
                {
                    return minifyResources && (String.IsNullOrEmpty(minificationExcludeRegex) || !Regex.IsMatch(path, minificationExcludeRegex));
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
            try
            {
                ISmartResource previousResource = null;
                foreach (var resource in smartResources)
                {
                    fullPath = resource.FullPath;

                    // Conditional resources are stored separately to apply conditions
                    // Resources that have the same condition and are after each other will be combined.
                    if (previousResource != null)
                    {
                        if (previousResource.IsConditional 
                            && previousResource.Settings.Condition != resource.Settings.Condition)
                        {
                            var conditionalResource = NewResource();
                            conditionalResource.FillRequiredContext("/Fake", resourceType); // Just so we can adjust settings
                            conditionalResource.Settings.Condition = previousResource.Settings.Condition;
                            saveCombination(conditionalResource);
                        }
                        else if (!previousResource.IsConditional && resource.IsConditional)
                        {
                            saveCombination(NewResource());
                        } 
                    }

                    // Ensuring the resource is a local one
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
                        saveCombination(NewResource());
                        resource.UrlOverride = resource.FullPath;
                        _cacheFileService.Save(hashCode, resource);
                    }

                    previousResource = resource;
                }
            }
            catch (Exception e)
            {
                var message = "Processing of resource " + fullPath + " failed";
                throw new ApplicationException(message, e);
            }

            saveCombination(NewResource());
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