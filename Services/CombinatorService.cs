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
using System.IO;
using System.Diagnostics;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorService : ICombinatorService
    {
        private readonly ICacheFileService _cacheFileService;
        private readonly IResourceFileService _resourceFileService;
        private readonly IMinificationService _minificationService;
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly ICacheManager _cacheManager;

        public ILogger Logger { get; set; }

        public CombinatorService(
            ICacheFileService cacheFileService,
            IResourceFileService resourceFileService,
            IMinificationService minificationService,
            IWorkContextAccessor workContextAccessor,
            ICacheManager cacheManager)
        {
            _cacheFileService = cacheFileService;
            _resourceFileService = resourceFileService;
            _minificationService = minificationService;
            _workContextAccessor = workContextAccessor;
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
                var smartResource = NewResource();
                smartResource.Type = resourceType;
                smartResource.FillRequiredContext(resource); // Copying the context so the original one won't be touched
                smartResources.Add(smartResource);
            }

            var combinedContent = new StringBuilder(resources.Count * 1000); // Rough estimate

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

                        ProcessResource(resource, combinedContent, settings);
                    }
                    else
                    {
                        if (previousResource != null) saveCombination(previousResource);
                        ProcessResource(resource, combinedContent, settings);
                        saveCombination(resource);
                        smartResources[i] = null;
                    }
                }
                catch (Exception e)
                {
                    throw new ApplicationException("Processing of resource " + publicUrl + " failed", e);
                }
            }


            saveCombination(smartResources[smartResources.Count - 1]);
        }

        // Todo: better name?
        private void ProcessResource(ISmartResource resource, StringBuilder combinedContent, ICombinatorSettings settings)
        {
            if (!resource.IsCDNResource || settings.CombineCDNResources)
            {
                if (!resource.IsCDNResource)
                {
                    resource.Content = _resourceFileService.GetLocalResourceContent(resource);
                }
                else if (settings.CombineCDNResources)
                {
                    resource.Content = _resourceFileService.GetRemoteResourceContent(resource);
                }

                if (resource.Type == ResourceType.Style)
                {
                    if (settings.EmbedCssImages)
                    {
                        EmbedImages(resource);
                    }
                    else
                    {
                        AdjustRelativePaths(resource);
                    }
                }

                if (settings.MinifyResources && (String.IsNullOrEmpty(settings.MinificationExcludeRegex) || !Regex.IsMatch(resource.PublicUrl.ToString(), settings.MinificationExcludeRegex)))
                {
                    MinifyResourceContent(resource);
                }

                combinedContent.Append(resource.Content);
            }
            else
            {
                resource.OverrideCombinedUrl(resource.PublicUrl);
            }
        }

        private void EmbedImages(ISmartResource resource)
        {
            // Uri is the key so that the key is uniform, inclusion urls are not
            var imageUrls = new Dictionary<Uri, string>();

            ProcessUrlSettings(resource,
                (match) =>
                {
                    var url = match.Groups[1].ToString();
                    var extension = Path.GetExtension(url).Replace(".", "").ToLowerInvariant();

                    // This is a dumb check but otherwise we'd have to inspect the file thoroughly
                    if ("jpg jpeg png gif tiff bmp".Contains(extension))
                    {
                        Uri imageUrl;
                        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) imageUrl = new Uri(resource.PublicUrl, url);
                        else imageUrl = new Uri(url);
                        imageUrls[imageUrl] = url;
                    }

                    return match.Groups[0].ToString();
                });

            var dataUrls = new List<Tuple<string, string>>(imageUrls.Count);

            foreach (var url in imageUrls)
            {
                try
                {
                    dataUrls.Add(new Tuple<string, string>(
                        url.Value,
                        "data:image/"
                            + Path.GetExtension(url.Key.ToString()).Replace(".", "")
                            +";base64,"
                            + _resourceFileService.GetImageBase64Data(url.Key))
                        ); 
                }
                catch (Exception e)
                {
                    throw new ApplicationException("The image with url " + url.Value + " can't be embedded", e);
                }
            }

            foreach (var url in dataUrls)
            {
                resource.Content = resource.Content.Replace(url.Item1, url.Item2);
            }
        }

        private static void AdjustRelativePaths(ISmartResource resource)
        {
            ProcessUrlSettings(resource,
                (match) =>
                {
                    var url = match.Groups[1].ToString();

                    return "url(\"" + new Uri(resource.PublicUrl, url) + "\")";
                });
        }

        private static void ProcessUrlSettings(ISmartResource resource, MatchEvaluator evaluator)
        {
            string content = resource.Content;

            content = Regex.Replace(
                                    content,
                                    "url\\(['|\"]?(.+?)['|\"]?\\)",
                                    evaluator,
                                    RegexOptions.IgnoreCase);

            resource.Content = content;
        }

        private void MinifyResourceContent(ISmartResource resource)
        {
            if (resource.Type == ResourceType.Style)
            {
                resource.Content = _minificationService.MinifyCss(resource.Content);
            }
            else if (resource.Type == ResourceType.JavaScript)
            {
                resource.Content = _minificationService.MinifyJavaScript(resource.Content);
            }
        }

        private IList<ResourceRequiredContext> ProcessCombinedResources(IList<ISmartResource> combinedResources)
        {
            return (from r in combinedResources select r.RequiredContext).ToList();
        }

        private ISmartResource NewResource()
        {
            return _workContextAccessor.GetContext().Resolve<ISmartResource>();
        }
    }
}