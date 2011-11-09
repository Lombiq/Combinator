using System.Collections.Generic;
using System.IO;
using System.Linq;
using Orchard.Caching;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.FileSystems.VirtualPath;
using Orchard.Services;
using Piedone.Combinator.Helpers;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CacheFileService : ICacheFileService
    {
        private readonly IVirtualPathProvider _virtualPathProvider;
        private readonly IRepository<CombinedFileRecord> _fileRepository;
        private readonly IClock _clock;

        #region In-memory caching fields
        private readonly ICacheManager _cacheManager;
        private readonly ISignals _signals;
        private const string CachePrefix = "Piedone.Combinator.";
        private const string CacheChangedSignal = "Associativy.Combinator.CacheChanged";
        #endregion

        #region Static file caching fields
        private bool _cacheFoldersExist = false;
        public const string cacheFolderName = "CombinedCache";
        private const string _rootPath = "~/Modules/Piedone.Combinator/";
        private const string _stylesPath = _rootPath + "Styles/" + cacheFolderName + "/";
        private const string _scriptsPath = _rootPath + "Scripts/" + cacheFolderName + "/";
        #endregion

        public CacheFileService(
            IVirtualPathProvider virtualPathProvider,
            IRepository<CombinedFileRecord> fileRepository,
            IClock clock,
            ICacheManager cacheManager,
            ISignals signals)
        {
            _fileRepository = fileRepository;
            _virtualPathProvider = virtualPathProvider;
            _clock = clock;

            _cacheManager = cacheManager;
            _signals = signals;
        }

        public string Save(int hashCode, ResourceType type, string content)
        {
            var scliceCount = _fileRepository.Count(file => file.HashCode == hashCode);

            var fileRecord = new CombinedFileRecord()
            {
                HashCode = hashCode,
                Slice = ++scliceCount,
                Type = type,
                LastUpdatedUtc = _clock.UtcNow
            };


            if (!_cacheFoldersExist)
            {
                _virtualPathProvider.CreateDirectory(_scriptsPath);
                _virtualPathProvider.CreateDirectory(_stylesPath);

                _cacheFoldersExist = true;
            }

            var path = MakePath(fileRecord);

            using (StreamWriter sw = _virtualPathProvider.CreateText(path))
            {
                sw.Write(content);
            }

            _fileRepository.Create(fileRecord);

            TriggerCacheChangedSignal();

            return MakePath(fileRecord);
        }

        public List<string> GetUrls(int hashCode)
        {
            return _cacheManager.Get(MakeCacheKey("GetUrls." + hashCode.ToString()), ctx =>
            {
                MonitorCacheChangedSignal(ctx);

                var files = GetRecords(hashCode);
                var fileCount = files.Count;

                var urls = new List<string>(fileCount);

                foreach (var file in files)
                {
                    urls.Add(MakePath(file));
                }

                return urls;
            });

        }

        public bool Exists(int hashCode)
        {
            return _cacheManager.Get(MakeCacheKey("Exists." + hashCode.ToString()), ctx =>
            {
                MonitorCacheChangedSignal(ctx);
                // Maybe also chek if the file exists?
                return _fileRepository.Count(file => file.HashCode == hashCode) != 0;
            });
        }

        public int GetCount()
        {
            return _fileRepository.Table.Count();
        }

        public void Delete(int hashCode, ResourceType type)
        {
            DeleteFiles(GetRecords(hashCode));

            TriggerCacheChangedSignal();
        }

        public void Empty()
        {
            // Not efficient (a truncate would be better), but is there any other way with IRepository?
            var files = _fileRepository.Table.ToList();
            foreach (var file in files)
            {
                _fileRepository.Delete(file);
            }

            if (files.Count() != 0)
            {
                if (_virtualPathProvider.DirectoryExists(_scriptsPath)) Directory.Delete(_virtualPathProvider.MapPath(_scriptsPath), true);
                if (_virtualPathProvider.DirectoryExists(_stylesPath)) Directory.Delete(_virtualPathProvider.MapPath(_stylesPath), true);
            }

            TriggerCacheChangedSignal();
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
                File.Delete(_virtualPathProvider.MapPath(MakePath(file)));
            }

            TriggerCacheChangedSignal();
        }

        private string MakePath(CombinedFileRecord file)
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

            return folderPath + file.HashCode + "-" + file.Slice + "." + extension;
        }

        #region In-memory caching methods
        private void MonitorCacheChangedSignal(AcquireContext<string> ctx)
        {
            ctx.Monitor(_signals.When(CacheChangedSignal));
        }

        private void TriggerCacheChangedSignal()
        {
            _signals.Trigger(CacheChangedSignal);
        }

        private string MakeCacheKey(string name)
        {
            return CachePrefix + name;
        }
        #endregion
    }
}