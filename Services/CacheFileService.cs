using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private bool _cacheFoldersExist = false;

        public const string cacheFolderName = "CombinedCache";
        private const string _rootPath = "~/Modules/Piedone.Combinator/";
        private const string _stylesPath = _rootPath + "Styles/" + cacheFolderName + "/";
        private const string _scriptsPath = _rootPath + "Scripts/" + cacheFolderName + "/";

        public CacheFileService(
            IVirtualPathProvider virtualPathProvider,
            IRepository<CombinedFileRecord> fileRepository,
            IClock clock)
        {
            _fileRepository = fileRepository;
            _virtualPathProvider = virtualPathProvider;
            _clock = clock;
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

            return MakePath(fileRecord);
        }

        public List<string> GetUrls(int hashCode)
        {
            var files = GetRecords(hashCode);
            var fileCount = files.Count;

            var urls = new List<string>(fileCount);

            foreach (var file in files)
            {
                urls.Add(MakePath(file));
            }

            return urls;
        }

        public bool Exists(int hashCode)
        {
            // Maybe also chek if the file exists?
            // This is ugly as hell, but currently there is no other way of checking with IStorageProvider if a file exists.
            //var exists = true;
            //try
            //{
            //    _storageProvider.GetPublicUrl(path);
            //}
            //catch (Exception)
            //{
            //    exists = false;
            //}
            return _fileRepository.Count(file => file.HashCode == hashCode) != 0;
        }

        public int GetCount()
        {
            return _fileRepository.Table.Count();
        }

        public void Delete(int hashCode, ResourceType type)
        {
            DeleteFiles(GetRecords(hashCode));
        }

        public void Empty()
        {
            // Not efficient, but is there any other way with IRepository?
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
    }
}