using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Environment.Extensions;
using Orchard.UI.Resources;
using Autofac.Features.Metadata;
using Orchard;
using System.Net;
using Orchard.FileSystems.Media;
using System.Text;
using System.IO;
using Orchard.Logging;

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
        private readonly IStorageProvider _storageProvider;
        private readonly IOrchardServices _orchardServices;

        public ILogger Logger { get; set; }

        public CombinedResourceManager(
            IEnumerable<Meta<IResourceManifestProvider>> resourceProviders,
            IStorageProvider storageProvider,
            IOrchardServices orchardServices)
            : base(resourceProviders)
        {
            _orchardServices = orchardServices;
            _storageProvider = storageProvider;

            Logger = NullLogger.Instance;
        }

        public override IList<ResourceRequiredContext> BuildRequiredResources(string resourceType)
        {
            var rootPath = "Piedone.Combinator/";

            // It's necessary to make a copy since making a change to the local variable also changes the private one. This is most likely some bug
            // with a reference that shouldn't be given away.
            var resources = new List<ResourceRequiredContext>(CleanCombinedResources(base.BuildRequiredResources(resourceType)));
            if (resources.Count == 0) return resources;

            var key = MakeResourceListKey(resources);

            try
            {
                if (resourceType == "stylesheet")
                {
                    if (!_combinedResources.ContainsKey(key))
                    {
                        _combinedResources[key] = Combine(resources, resourceType, rootPath + "Styles/" + key + ".css");
                    }

                    return _combinedResources[key];
                }
                else if (resourceType == "script")
                {
                    var combinedScripts = new List<ResourceRequiredContext>(2);

                    Action<ResourceLocation> combineScriptsAtLocation =
                        (location) =>
                        {
                            var resourceKey = key + (int)location;
                            var dk = _combinedResources.ContainsKey(resourceKey);
                            if (!_combinedResources.ContainsKey(resourceKey))
                            {
                                var scripts = (from r in resources
                                               where r.Settings.Location == location
                                               select r).ToList();


                                if (scripts.Count != 0)
                                {
                                    _combinedResources[resourceKey] = Combine(scripts, "script", rootPath + "Scripts/" + resourceKey + ".js");
                                    SetLocation(_combinedResources[resourceKey], location);
                                    combinedScripts = combinedScripts.Union(_combinedResources[resourceKey]).ToList();
                                }
                            }
                            else combinedScripts = combinedScripts.Union(_combinedResources[resourceKey]).ToList();
                        };

                    combineScriptsAtLocation(ResourceLocation.Head);
                    combineScriptsAtLocation(ResourceLocation.Foot);

                    return combinedScripts;
                }

                return base.BuildRequiredResources(resourceType);
            }
            catch (Exception e)
            {
                // There was some problem with reading a file
                Logger.Error(e, "Error when combining " + resourceType + " files");
                return base.BuildRequiredResources(resourceType);
            }
        }

        /// <summary>
        /// Combines the content of resources
        /// </summary>
        /// <param name="resources">Resources to combine</param>
        /// <param name="resourceType">Type of the resources (e.g. script, stylesheet...)</param>
        /// <param name="savePath">Relative path in the Media folder to save the combined file</param>
        /// <returns>Most of the times the single combined content, but can return more if some of them couldn't be
        /// combined (e.g. was not found or is not a local resource)</returns>
        private IList<ResourceRequiredContext> Combine(IList<ResourceRequiredContext> resources, string resourceType, string savePath)
        {
            var baseUri = new Uri(_orchardServices.WorkContext.CurrentSite.BaseUrl, UriKind.Absolute);
            var wc = new WebClient();
            var combinedContent = "";
            var partCount = 1;

            Action<string, int> save =
                (content, insertIndex) =>
                {
                    if (combinedContent.Length == 0) return;

                    var path = savePath.Insert(savePath.LastIndexOf('.'), "-" + partCount); // Inserts part count

                    // EZ EGYÉBKÉNT NEM KELL!!!!!!!!!
                    // Mert mindig felülírja...
                    try
                    {
                        _storageProvider.DeleteFile(path);
                    }
                    catch (Exception)
                    {
                        // This is the first time the module is run and the file wasn't yet created. Since there is no way to check its existence,
                        // this is the only way to "overwrite" it.
                    }

                    _storageProvider.SaveStream(
                        path,
                        new MemoryStream(
                            new System.Text.UTF8Encoding().GetBytes(
                                content
                                )
                            )
                        );

                    var combined = new ResourceRequiredContext();
                    var url = _storageProvider.GetPublicUrl(path);
                    combined.Settings = Include(resourceType, url, url);
                    combined.Resource = FindResource(combined.Settings);

                    if(insertIndex == -1) resources.Add(combined);
                    else resources.Insert(insertIndex, combined);

                    partCount++;
                    combinedContent = "";
                };

            var fullPath = "";
            for (int i = 0; i < resources.Count; i++)
            {
                try
                {
                    fullPath = GetResourceFullPath(resources[i].Resource);
                    // Ensuring the resource is a local one and that it hasn't some conditions
                    if (!Uri.IsWellFormedUriString(fullPath, UriKind.Absolute) && String.IsNullOrEmpty(resources[i].Settings.Condition))
                    {
                        if (fullPath.StartsWith(baseUri.AbsolutePath)) fullPath = fullPath.Replace(baseUri.AbsolutePath, ""); // Strip e.g. /OrchardLocal
                        else fullPath = fullPath.Replace("~", ""); // Strip the tilde from ~/Modules/...
                        fullPath = baseUri.AbsoluteUri + fullPath;

                        combinedContent += wc.DownloadString(fullPath); // It seems that it's not possible to read the local files
                        resources.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        // This is to ensure that if there's a remote resource inside a list of local resources, their order stays
                        // the same (so the product is: localResourcesCombined1, remoteResource, localResourceCombined2...)
                        save(combinedContent, i);
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


            save(combinedContent, -1);

            return resources;
        }

        private int MakeResourceListKey(IList<ResourceRequiredContext> resources)
        {
            var key = "";

            resources.ToList().ForEach(resource => key += GetResourceFullPath(resource.Resource) + "__");

            return key.GetHashCode();
        }

        private string GetResourceFullPath(ResourceDefinition resource)
        {
            return resource.BasePath + resource.Url;
        }

        private IList<ResourceRequiredContext> SetLocation(IList<ResourceRequiredContext> resources, ResourceLocation location)
        {
            resources.ToList().ForEach(resource => resource.Settings.Location = location);
            return resources;
        }

        private IList<ResourceRequiredContext> CleanCombinedResources(IList<ResourceRequiredContext> resources)
        {
            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].Resource.Url.Contains("/Media/Default/Piedone.Combinator"))
                {
                    resources.RemoveAt(i);
                    i--;
                }
            }
            return resources;
        }
    }
}