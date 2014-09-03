using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Orchard;
using Orchard.Caching;
using Orchard.Caching.Services;
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

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CacheFileService : ICacheFileService, ICombinatorCacheManipulationEventHandler
    {
        private readonly IOrchardHost _orchardHost;
        private readonly IStorageProvider _storageProvider;
        private readonly IRepository<CombinedFileRecord> _fileRepository;
        private readonly ICombinatorResourceManager _combinatorResourceManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IClock _clock;
        private readonly ICombinatorEventHandler _combinatorEventHandler;

        #region In-memory caching fields
        private readonly ICacheService _cacheService;
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
            IHttpContextAccessor httpContextAccessor,
            IClock clock,
            ICombinatorEventHandler combinatorEventHandler,
            ICacheService cacheService,
            ICombinatorEventMonitor combinatorEventMonitor)
        {
            _orchardHost = orchardHost;
            _storageProvider = storageProvider;
            _fileRepository = fileRepository;
            _combinatorResourceManager = combinatorResourceManager;
            _httpContextAccessor = httpContextAccessor;
            _clock = clock;
            _combinatorEventHandler = combinatorEventHandler;

            _cacheService = cacheService;
            _combinatorEventMonitor = combinatorEventMonitor;
        }


        public void Save(string fingerprint, CombinatorResource resource, Uri resourceBaseUri, bool useResourceShare)
        {
            if (useResourceShare && CallOnDefaultShell(cacheFileService => cacheFileService.Save(fingerprint, resource, resourceBaseUri, false)))
            {
                return;
            }

            var sliceCount = _fileRepository.Count(file => file.Fingerprint == fingerprint);

            var fileRecord = new CombinedFileRecord()
            {
                Fingerprint = fingerprint,
                Slice = ++sliceCount,
                Type = resource.Type,
                LastUpdatedUtc = _clock.UtcNow,
                Settings = _combinatorResourceManager.SerializeResourceSettings(resource)
            };

            _fileRepository.Create(fileRecord);

            if (!String.IsNullOrEmpty(resource.Content))
            {
                var path = MakePath(fileRecord);

                if (_storageProvider.FileExists(path)) _storageProvider.DeleteFile(path);

                using (var stream = _storageProvider.CreateFile(path).OpenWrite())
                {
                    var bytes = Encoding.UTF8.GetBytes(resource.Content);
                    stream.Write(bytes, 0, bytes.Length);
                }

                // This is needed to adjust relative paths if the resource is stored in a remote storage provider.
                // Why the double-saving? Before saving the file there is no reliable way to tell whether the storage public url will be a
                // remote one or not...
                var testResource = _combinatorResourceManager.ResourceFactory(resource.Type);
                testResource.FillRequiredContext("TestCombinedResource", _storageProvider.GetPublicUrl(path));
                _combinatorResourceManager.DeserializeSettings(fileRecord.Settings, testResource);
                if (testResource.IsRemoteStorageResource)
                {
                    _storageProvider.DeleteFile(path);

                    testResource.Content = resource.Content;
                    ResourceProcessingService.RegexConvertRelativeUrlsToAbsolute(testResource, resourceBaseUri);

                    using (var stream = _storageProvider.CreateFile(path).OpenWrite())
                    {
                        var bytes = Encoding.UTF8.GetBytes(testResource.Content);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
            }

            _combinatorEventHandler.BundleChanged(fingerprint);
        }

        public IList<CombinatorResource> GetCombinedResources(string fingerprint, bool useResourceShare)
        {
            IList<CombinatorResource> sharedResources = new List<CombinatorResource>();
            if (useResourceShare)
            {
                CallOnDefaultShell(cacheFileService => sharedResources = cacheFileService.GetCombinedResources(fingerprint, false));
            }

            var cacheKey = MakeCacheKey("GetCombinedResources." + fingerprint);
            return _cacheService.Get(cacheKey, () =>
            {
                _combinatorEventMonitor.MonitorCacheEmptied(cacheKey);
                _combinatorEventMonitor.MonitorBundleChanged(cacheKey, fingerprint);

                var files = _fileRepository.Fetch(file => file.Fingerprint == fingerprint).ToList();
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

                    resource.LastUpdatedUtc = file.LastUpdatedUtc ?? _clock.UtcNow;
                    resources.Add(resource);
                }

                // This way if a set of resources contains shared and local resources in two resource sets then both will be returned.
                // Issue: the order is not necessarily correctly kept... But this should be a rare case. Should be solved eventually.
                return sharedResources.Union(resources).ToList();
            });
        }

        public bool Exists(string fingerprint, bool useResourceShare)
        {
            var exists = false;
            if (useResourceShare && CallOnDefaultShell(cacheFileService => exists = cacheFileService.Exists(fingerprint, false)))
            {
                // Because resources were excluded from resource sharing in this set this set could be stored locally, not shared.
                // Thus we fall back to local storage.
                if (exists) return true;
            }

            var cacheKey = MakeCacheKey("Exists." + fingerprint);
            return _cacheService.Get(cacheKey, () =>
            {
                _combinatorEventMonitor.MonitorCacheEmptied(cacheKey);
                _combinatorEventMonitor.MonitorBundleChanged(cacheKey, fingerprint);
                // Maybe also check if the file exists?
                return _fileRepository.Count(file => file.Fingerprint == fingerprint) != 0;
            });
        }

        public int GetCount()
        {
            return _fileRepository.Table.Count();
        }

        public void Empty()
        {
            var files = _fileRepository.Table.ToList();

            foreach (var file in files)
            {
                _fileRepository.Delete(file);
            }

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
                if (!wc.TryResolve<ICacheFileService>(out cacheFileService)) return false;
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


        private static string MakeCacheKey(string name)
        {
            return CachePrefix + name;
        }
    }
}