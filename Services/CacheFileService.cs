using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Orchard;
using Orchard.Caching;
using Orchard.Data;
using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.Exceptions;
using Orchard.FileSystems.Media;
using Orchard.Mvc;
using Orchard.Services;
using Piedone.Combinator.EventHandlers;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Models;
using Autofac;
using System.Web.Mvc;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CacheFileService : ICacheFileService, ICombinatorCacheManipulationEventHandler
    {
        private readonly IOrchardHost _orchardHost;
        private readonly IStorageProvider _storageProvider;
        private readonly IRepository<CombinedFileRecord> _fileRepository;
        private readonly ICombinatorResourceManager _combinatorResourceManager;
        private readonly UrlHelper _urlHelper;
        private readonly IClock _clock;
        private readonly ITransactionManager _transactionManager;
        private readonly ICombinatorEventHandler _combinatorEventHandler;
        private readonly ShellSettings _shellSettings;

        #region In-memory caching fields
        private readonly ICacheManager _cacheManager;
        private readonly ICombinatorEventMonitor _combinatorEventMonitor;
        private const string CachePrefix = "Piedone.Combinator.";
        #endregion

        #region Static file caching properties
        private string RootPath { get { return _storageProvider.Combine("_PiedoneModules", "Combinator"); } }
        private string StylesPath { get { return _storageProvider.Combine(RootPath, "Styles"); } }
        private string ScriptsPath { get { return _storageProvider.Combine(RootPath, "Scripts"); } }
        #endregion


        public CacheFileService(
            IOrchardHost orchardHost,
            IStorageProvider storageProvider,
            IRepository<CombinedFileRecord> fileRepository,
            ICombinatorResourceManager combinatorResourceManager,
            UrlHelper urlHelper,
            IClock clock,
            ITransactionManager transactionManager,
            ICombinatorEventHandler combinatorEventHandler,
            ICacheManager cacheManager,
            ICombinatorEventMonitor combinatorEventMonitor,
            ShellSettings shellSettings)
        {
            _orchardHost = orchardHost;
            _storageProvider = storageProvider;
            _fileRepository = fileRepository;
            _combinatorResourceManager = combinatorResourceManager;
            _urlHelper = urlHelper;
            _clock = clock;
            _transactionManager = transactionManager;
            _combinatorEventHandler = combinatorEventHandler;
            _shellSettings = shellSettings;

            _cacheManager = cacheManager;
            _combinatorEventMonitor = combinatorEventMonitor;
        }


        public void Save(string fingerprint, CombinatorResource resource, ICombinatorSettings settings)
        {
            if (settings.EnableResourceSharing && CallOnDefaultShell(cacheFileService => cacheFileService.Save(fingerprint, resource, new CombinatorSettings(settings) { EnableResourceSharing = false })))
            {
                return;
            }

            var sliceCount = _fileRepository.Count(file => file.Fingerprint == ConvertFingerprintToStorageFormat(fingerprint));

            if (resource.LastUpdatedUtc == DateTime.MinValue)
            {
                resource.LastUpdatedUtc = _clock.UtcNow;
            }

            // Ceil-ing timestamp to the second, because sub-second precision is not stored in the DB. This would cause
            // a discrepancy between saved and fetched vs freshly created date times, causing unwanted cache busting
            // for the same resource.
            resource.LastUpdatedUtc = new DateTime(resource.LastUpdatedUtc.Year, resource.LastUpdatedUtc.Month, resource.LastUpdatedUtc.Day, resource.LastUpdatedUtc.Hour, resource.LastUpdatedUtc.Minute, resource.LastUpdatedUtc.Second);

            var fileRecord = new CombinedFileRecord()
            {
                Fingerprint = ConvertFingerprintToStorageFormat(fingerprint),
                Slice = ++sliceCount,
                Type = resource.Type,
                LastUpdatedUtc = resource.LastUpdatedUtc,
                Settings = _combinatorResourceManager.SerializeResourceSettings(resource)
            };

            _fileRepository.Create(fileRecord);

            if (!string.IsNullOrEmpty(resource.Content))
            {
                var path = MakePath(fileRecord);

                if (_storageProvider.FileExists(path)) _storageProvider.DeleteFile(path);

                using (var stream = _storageProvider.CreateFile(path).OpenWrite())
                {
                    var bytes = Encoding.UTF8.GetBytes(resource.Content);
                    stream.Write(bytes, 0, bytes.Length);
                }

                if (!resource.IsRemoteStorageResource)
                {
                    // This is needed to adjust relative paths if the resource is stored in a remote storage provider.
                    // Why the double-saving? Before saving the file there is no reliable way to tell whether the
                    // storage public url will be a remote one or not...
                    var testResource = _combinatorResourceManager.ResourceFactory(resource.Type);
                    testResource.FillRequiredContext("TestCombinedResource", _storageProvider.GetPublicUrl(path));
                    _combinatorResourceManager.DeserializeSettings(fileRecord.Settings, testResource);
                    testResource.IsRemoteStorageResource = settings.RemoteStorageUrlPattern != null && settings.RemoteStorageUrlPattern.IsMatch(testResource.AbsoluteUrl.ToString());
                    if (testResource.IsRemoteStorageResource)
                    {
                        _storageProvider.DeleteFile(path);

                        testResource.Content = resource.Content;
                        var relativeUrlsBaseUri = settings.ResourceBaseUri != null ? settings.ResourceBaseUri : new Uri(_urlHelper.RequestContext.HttpContext.Request.Url, _urlHelper.Content("~/"));
                        ResourceProcessingService.RegexConvertRelativeUrlsToAbsolute(testResource, relativeUrlsBaseUri);

                        using (var stream = _storageProvider.CreateFile(path).OpenWrite())
                        {
                            var bytes = Encoding.UTF8.GetBytes(testResource.Content);
                            stream.Write(bytes, 0, bytes.Length);
                        }

                        resource.IsRemoteStorageResource = true;
                        fileRecord.Settings = _combinatorResourceManager.SerializeResourceSettings(resource);
                    }
                }
            }

            _combinatorEventHandler.BundleChanged(fingerprint);
        }

        public IList<CombinatorResource> GetCombinedResources(string fingerprint, ICombinatorSettings settings)
        {
            IList<CombinatorResource> sharedResources = new List<CombinatorResource>();
            if (settings.EnableResourceSharing)
            {
                CallOnDefaultShell(cacheFileService => sharedResources = cacheFileService.GetCombinedResources(fingerprint, new CombinatorSettings(settings) { EnableResourceSharing = false }));
            }

            var cacheKey = MakeCacheKey("GetCombinedResources." + fingerprint);
            return _cacheManager.Get(cacheKey, acquireContext =>
            {
                _combinatorEventMonitor.MonitorCacheEmptied(acquireContext);
                _combinatorEventMonitor.MonitorBundleChanged(acquireContext, fingerprint);

                var files = _fileRepository.Fetch(file => file.Fingerprint == ConvertFingerprintToStorageFormat(fingerprint)).ToList();
                var fileCount = files.Count;

                var resources = new List<CombinatorResource>(fileCount);

                foreach (var file in files)
                {
                    var resource = _combinatorResourceManager.ResourceFactory(file.Type);

                    resource.FillRequiredContext("CombinedResource" + file.Id.ToString(), string.Empty);
                    _combinatorResourceManager.DeserializeSettings(file.Settings, resource);
                    if (!resource.IsOriginal) // If the resource is original its url was filled from the settings
                    {
                        resource.RequiredContext.Resource.SetUrl(_storageProvider.GetPublicUrl(MakePath(file)));
                    }

                    resource.LastUpdatedUtc = file.LastUpdatedUtc.HasValue ? file.LastUpdatedUtc.Value : _clock.UtcNow;
                    resources.Add(resource);
                }

                // This way if a set of resources contains shared and local resources in two resource sets then both will be returned.
                // Issue: the order is not necessarily correctly kept... But this should be a rare case. Should be solved eventually.
                return sharedResources.Union(resources).ToList();
            });
        }

        public bool Exists(string fingerprint, bool checkSharedResource)
        {
            if (_shellSettings.Name != ShellSettings.DefaultName)
            {
                var sharedResourceExists = false;
                if (checkSharedResource && CallOnDefaultShell(cacheFileService => sharedResourceExists = cacheFileService.Exists(fingerprint, false)))
                {
                    return sharedResourceExists;
                } 
            }

            var cacheKey = MakeCacheKey("Exists." + fingerprint);
            return _cacheManager.Get(cacheKey, acquireContext =>
            {
                _combinatorEventMonitor.MonitorCacheEmptied(acquireContext);
                _combinatorEventMonitor.MonitorBundleChanged(acquireContext, fingerprint);
                // Maybe also check if the file exists?
                return _fileRepository.Count(file => file.Fingerprint == ConvertFingerprintToStorageFormat(fingerprint)) != 0;
            });
        }

        public int GetCount()
        {
            return _fileRepository.Table.Count();
        }

        public void Empty()
        {
            _transactionManager.GetSession().CreateQuery("DELETE FROM " + typeof(CombinedFileRecord).FullName).ExecuteUpdate();

            if (_storageProvider.FolderExists(RootPath))
            {
                _storageProvider.DeleteFolder(RootPath);
            }

            _combinatorEventHandler.CacheEmptied();
        }

        void ICombinatorCacheManipulationEventHandler.EmptyCache()
        {
            Empty();
        }

        public void WriteSpriteStream(string fileName, SpriteStreamWriter streamWriter)
        {
            var path = StylesPath + "Sprites/" + fileName;
            if (_storageProvider.FileExists(path)) _storageProvider.DeleteFile(path);
            var spriteFile = _storageProvider.CreateFile(path);
            var publicUrl = _storageProvider.GetPublicUrl(path);
            using (var stream = spriteFile.OpenWrite())
            {
                streamWriter(stream, publicUrl);
            }
        }

        private bool CallOnDefaultShell(Action<ICacheFileService> cacheFileServiceCall)
        {
            var shellContext = _orchardHost.GetShellContext(new ShellSettings { Name = ShellSettings.DefaultName });
            if (shellContext == null) throw new InvalidOperationException("The Default tenant's shell context does not exist. This most possibly indicates that the shell is not running. Combinator resource sharing needs the Default tenant to run.");
            using (var wc = shellContext.LifetimeScope.Resolve<IWorkContextAccessor>().CreateWorkContextScope())
            {
                ICacheFileService cacheFileService;
                if (!wc.TryResolve(out cacheFileService)) return false;
                cacheFileServiceCall(cacheFileService);
                return true;
            }
        }

        private string MakePath(CombinedFileRecord file)
        {
            // Maybe others will come, therefore the architecture
            string extension = "";
            string folderPath = "";
            if (file.Type == ResourceType.JavaScript)
            {
                folderPath = ScriptsPath;
                extension = "js";
            }
            else if (file.Type == ResourceType.Style)
            {
                folderPath = StylesPath;
                extension = "css";
            }

            return _storageProvider.Combine(folderPath, file.GetFileName() + "." + extension);
        }


        public static string ConvertFingerprintToStorageFormat(string fingerprint)
        {
            return fingerprint.GetHashCode().ToString();
        }

        private static string MakeCacheKey(string name)
        {
            return CachePrefix + name;
        }
    }
}