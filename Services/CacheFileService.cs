using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Orchard.Caching;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.FileSystems.Media;
using Orchard.Services;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
using Piedone.Combinator.Models;
using Piedone.HelpfulLibraries.DependencyInjection;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CacheFileService : ICacheFileService
    {
        private readonly IStorageProvider _storageProvider;
        private readonly IRepository<CombinedFileRecord> _fileRepository;
        private readonly IClock _clock;
        private readonly IResolve<ISmartResource> _smartResourceResolve;
        
        private static readonly object saveLocker = new object();

        #region In-memory caching fields
        private readonly ICacheManager _cacheManager;
        private readonly ISignals _signals;
        private const string CachePrefix = "Piedone.Combinator.";
        private const string CacheChangedSignal = "Associativy.Combinator.CacheChanged";
        #endregion

        #region Static file caching fields
        private const string _rootPath = "Combinator/";
        private const string _stylesPath = _rootPath + "Styles/";
        private const string _scriptsPath = _rootPath + "Scripts/";
        #endregion

        public CacheFileService(
            IStorageProvider storageProvider,
            IRepository<CombinedFileRecord> fileRepository,
            IClock clock,
            IResolve<ISmartResource> smartResourceResolve,
            ICacheManager cacheManager,
            ISignals signals)
        {
            _fileRepository = fileRepository;
            _storageProvider = storageProvider;
            _clock = clock;
            _smartResourceResolve = smartResourceResolve;

            _cacheManager = cacheManager;
            _signals = signals;
        }

        public void Save(int hashCode, ISmartResource resource)
        {
            lock (saveLocker)
            {
                var sliceCount = _fileRepository.Count(file => file.HashCode == hashCode);
                var nextSlice = sliceCount + 1;

                // So we don't overwrite
                if (Exists(hashCode, nextSlice)) return;

                var fileRecord = new CombinedFileRecord()
                {
                    HashCode = hashCode,
                    Slice = nextSlice,
                    Type = resource.Type,
                    LastUpdatedUtc = _clock.UtcNow,
                    Settings = resource.GetSerializedSettings()
                };

                if (!String.IsNullOrEmpty(resource.Content))
                {
                    var path = MakePath(fileRecord);

                    using (var stream = _storageProvider.CreateFile(path).OpenWrite())
                    {
                        var bytes = Encoding.UTF8.GetBytes(resource.Content);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                _fileRepository.Create(fileRecord);

                TriggerCacheChangedSignal(hashCode);
            }
        }

        public IList<ISmartResource> GetCombinedResources(int hashCode)
        {
            return _cacheManager.Get(MakeCacheKey("GetPublicUrls." + hashCode.ToString()), ctx =>
            {
                MonitorCacheChangedSignal(ctx, hashCode);

                var files = GetRecords(hashCode);
                var fileCount = files.Count;

                var resources = new List<ISmartResource>(fileCount);

                foreach (var file in files)
                {
                    var resource = _smartResourceResolve.Value;
                    resource.Type = file.Type;
                    resource.FillRequiredContext(_storageProvider.GetPublicUrl(MakePath(file)), file.Settings);
                    resource.LastUpdatedUtc = file.LastUpdatedUtc ?? _clock.UtcNow;
                    resources.Add(resource);
                }

                return resources;
            });
        }

        public bool Exists(int hashCode)
        {
            return _cacheManager.Get(MakeCacheKey("Exists." + hashCode.ToString()), ctx =>
            {
                MonitorCacheChangedSignal(ctx, hashCode);
                // Maybe also check if the file exists?
                return _fileRepository.Count(file => file.HashCode == hashCode) != 0;
            });
        }

        public int GetCount()
        {
            return _fileRepository.Table.Count();
        }

        public void Delete(int hashCode)
        {
            DeleteFiles(GetRecords(hashCode));

            TriggerCacheChangedSignal(hashCode);
        }

        public void Empty()
        {
            var files = _fileRepository.Table.ToList();
            DeleteFiles(files);

            if (files.Count() != 0)
            {
                try
                {
                    // These will throw an exception if a folder doesn't exist. Since currently there is no method
                    // in IStorageProvider to check the existence of a file/folder (see: http://orchard.codeplex.com/discussions/275146)
                    // this is the only way to deal with it.
                    _storageProvider.DeleteFolder(_scriptsPath);
                    Thread.Sleep(300); // This is to ensure we don't get an "access denied" when deleting the root folder
                    _storageProvider.DeleteFolder(_stylesPath);
                    Thread.Sleep(300);
                }
                catch (Exception)
                {
                }
                _storageProvider.DeleteFolder(_rootPath);
            }

            TriggerCacheChangedSignal();
        }

        private bool Exists(int hashCode, int slice)
        {
            return _fileRepository.Count(file => file.HashCode == hashCode && file.Slice == slice) == 1;
        }

        private List<CombinedFileRecord> GetRecords(int hashCode)
        {
            return _fileRepository.Fetch(file => file.HashCode == hashCode).ToList();
        }

        private void DeleteFiles(List<CombinedFileRecord> files)
        {
            foreach (var file in files)
            {
                _fileRepository.Delete(file);
                // Try-catch for the case that someone deleted the file.
                // Currently there is no way to check the existance of a file.
                try
                {
                    _storageProvider.DeleteFile(MakePath(file));
                }
                catch (Exception)
                {
                }
            }
        }

        private static string MakePath(CombinedFileRecord file)
        {
            // Maybe others will come, therefore the architecture
            string extension = "";
            string folderPath = "";
            if (file.Type == ResourceType.JavaScript)
            {
                folderPath = _scriptsPath;
                extension = "js";
            }
            else if (file.Type == ResourceType.Style)
            {
                folderPath = _stylesPath;
                extension = "css";
            }

            return folderPath + file.GetFileName() + "." + extension;
        }

        #region In-memory caching methods
        public void MonitorCacheChangedSignal(AcquireContext<string> ctx, int hashCode)
        {
            ctx.Monitor(_signals.When(MakeCacheChangedSignal(hashCode)));
            ctx.Monitor(_signals.When(CacheChangedSignal));
        }

        /// <summary>
        /// Trigger for the whole cache
        /// </summary>
        private void TriggerCacheChangedSignal()
        {
            _signals.Trigger(CacheChangedSignal);
        }

        /// <summary>
        /// Trigger for parts of the cache corresponding to set of resources
        /// </summary>
        /// <param name="hashCode">Hash of the resources set</param>
        private void TriggerCacheChangedSignal(int hashCode)
        {
            _signals.Trigger(MakeCacheChangedSignal(hashCode));
        }

        private static string MakeCacheKey(string name)
        {
            return CachePrefix + name;
        }

        private static string MakeCacheChangedSignal(int hashCode)
        {
            return CacheChangedSignal + "." + hashCode.ToString();
        }
        #endregion
    }
}