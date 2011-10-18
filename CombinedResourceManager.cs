using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Autofac.Features.Metadata;
using Orchard;
using Orchard.Environment.Extensions;
using Orchard.Logging;
using Orchard.UI.Resources;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
using Piedone.Combinator.Services;
using Piedone.Combinator.Models;
using Orchard.ContentManagement; // For generic ContentManager methods
using System.Text;
using System.Text.RegularExpressions;

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
        private readonly IOrchardServices _orchardServices;
        private readonly ICacheFileService _cacheFileService;
        private IList<ResourceRequiredContext> _stylesheetResources = new List<ResourceRequiredContext>();

        public ILogger Logger { get; set; }

        public CombinedResourceManager(
            IEnumerable<Meta<IResourceManifestProvider>> resourceProviders,
            IOrchardServices orchardServices,
            ICacheFileService cacheFileService)
            : base(resourceProviders)
        {
            _orchardServices = orchardServices;
            _cacheFileService = cacheFileService;

            Logger = NullLogger.Instance;
        }

        /// <summary>
        /// Necessary as stylesheets can be overridden from themes
        /// 
        /// So the actual stylesheet resources can only be set from a response filter by getting it
        /// from the output.
        /// </summary>
        /// <param name="urls"></param>
        public void CombineStylesheets(List<string> urls)
        {
            _stylesheetResources = MakeResourcesFromPublicUrls(urls, ResourceType.Style);
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
                            // Now we don't combine here, but in CombineStylesheets(), invoked from StylesheetFilter.Write()
                            // _combinedResources[hashCode] = Combine(resources, hashCode, resourceType, settings.CombineCDNResources);
                        }
                        else
                        {
                            _combinedResources[hashCode] = MakeResourcesFromPublicUrls(_cacheFileService.GetPublicUrls(hashCode), resources, resourceType, settings.CombineCDNResources);
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
                                    _combinedResources[locationHashCode] = MakeResourcesFromPublicUrls(_cacheFileService.GetPublicUrls(locationHashCode), scripts, resourceType, settings.CombineCDNResources);
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
            var webClient = new WebClient();
            var combinedContent = new StringBuilder(10000);

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

            var byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            var encoding = new System.Text.UTF8Encoding(false);
            Action<string, int> downloadContent =
                (url, resourceIndex) =>
                {
                    //var content = webClient.DownloadString(url);
                    var content = encoding.GetString(webClient.DownloadData(url));
                    if (content.StartsWith(byteOrderMarkUtf8)) // Stripping "?"s from the beginning of css commments "/*"
                    {
                        content = content.Remove(0, byteOrderMarkUtf8.Length);
                    }

                    // Jumping up a directory
                    var uriSegments = new Uri(url).Segments; // Path class is not good for this
                    var parentDir = String.Join("", uriSegments.Take(uriSegments.Length - 2).ToArray());
                    content = Regex.Replace(content, Regex.Escape("../images/"), parentDir + "Images/", RegexOptions.IgnoreCase);

                    combinedContent.Append(content); // It seems that it's not possible to read local files directly
                    resources.RemoveAt(resourceIndex);
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
                                // Strip e.g. /OrchardLocal or even the first slash
                                // Finds the first occurence and replaces it with empty string
                                int Place = fullPath.IndexOf(baseUri.AbsolutePath);
                                fullPath = fullPath.Remove(Place, baseUri.AbsolutePath.Length).Insert(Place, "");
                            }
                            else fullPath = fullPath.Replace("~", ""); // Strip the tilde from ~/Modules/...
                            fullPath = baseUri.AbsoluteUri + fullPath;

                            downloadContent(fullPath, i);
                            i--;
                        }
                        else if (combineCDNResources)
                        {
                            downloadContent(fullPath, i);
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
            if (combineCDNResources) return MakeResourcesFromPublicUrlsWithCDNCombination(urls, resources, type);
            return MakeResourcesFromPublicUrls(urls, type);
        }

        private IList<ResourceRequiredContext> MakeResourcesFromPublicUrls(IList<string> urls, ResourceType type)
        {
            var resources = new List<ResourceRequiredContext>(urls.Count);

            foreach (var url in urls)
            {
                resources.Add(MakeResourceFromPublicUrl(url, type));
            }

            return resources;
        }

        private IList<ResourceRequiredContext> MakeResourcesFromPublicUrlsWithCDNCombination(IList<string> urls, IList<ResourceRequiredContext> resources, ResourceType type)
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