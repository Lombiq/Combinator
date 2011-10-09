using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.FileSystems.Media;
using Orchard.Services;
using Piedone.Combinator.Helpers;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    [OrchardFeature("Piedone.Combinator")]
    public class CacheFileService : ICacheFileService
    {
        private readonly IStorageProvider _storageProvider;
        private readonly IRepository<CombinedFileRecord> _fileRepository;
        private readonly IClock _clock;

        private const string _rootPath = "Piedone.Combinator/";
        private const string _stylesPath = _rootPath + "Styles/";
        private const string _scriptsPath = _rootPath + "Scripts/";

        public CacheFileService(
            IStorageProvider storageProvider,
            IRepository<CombinedFileRecord> fileRepository,
            IClock clock)
        {
            _fileRepository = fileRepository;
            _storageProvider = storageProvider;
            _clock = clock;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="content"></param>
        /// <param name="type"></param>
        /// <returns>The public URL of the file</returns>
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

            var path = MakePath(fileRecord);

            _storageProvider.SaveStream(
                        path,
                        new MemoryStream(
                            new System.Text.UTF8Encoding().GetBytes(
                                content
                                )
                            )
                        );

            _fileRepository.Create(fileRecord);

            return _storageProvider.GetPublicUrl(path);
        }

        public List<string> GetPublicUrls(int hashCode)
        {
            var files = GetRecords(hashCode);
            var fileCount = files.Count;

            var urls = new List<string>(fileCount);

            foreach (var file in files)
            {
                urls.Add(_storageProvider.GetPublicUrl(MakePath(file)));
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
            var records = _fileRepository.Table.ToList();
            DeleteFiles(records);

            if (records.Count() != 0)
            {
                try
                {
                    // These will throw an exception if a folder doesn't exist. Since currently there is no method
                    // in IStorageProvider to check the existence of a file/folder (see: http://orchard.codeplex.com/discussions/275146)
                    // this is the only way to deal with it.
                    _storageProvider.DeleteFolder(_scriptsPath);
                    _storageProvider.DeleteFolder(_stylesPath);
                }
                catch (Exception)
                {
                }
                _storageProvider.DeleteFolder(_rootPath);
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
                _storageProvider.DeleteFile(MakePath(file));
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