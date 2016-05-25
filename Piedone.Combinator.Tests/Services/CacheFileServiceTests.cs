using System;
using System.Collections.Generic;
using System.IO;
using Autofac;
using Moq;
using NUnit.Framework;
using Orchard.Caching;
using Orchard.Data;
using Orchard.Environment.Configuration;
using Orchard.FileSystems.Media;
using Orchard.Services;
using Orchard.Tests.Modules;
using Orchard.Tests.Stubs;
using Orchard.Tests.Utility;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;
using System.Linq;
using Piedone.Combinator.Tests.Stubs;
using Orchard.Caching.Services;
using System.Web.Mvc;

namespace Piedone.Combinator.Tests.Services
{
    [TestFixture]
    public class CacheFileServiceTests : DatabaseEnabledTestsBase
    {
        private const string _jsResourceFingerprint = "664456";
        private const string _cssResourcesFingerprint = "1151";

        private ResourceRepository _resourceRepository;
        private ICacheFileService _cacheFileService;

        protected override IEnumerable<Type> DatabaseTypes
        {
            get
            {
                return new[]
                {
                    typeof(CombinedFileRecord)
                };
            }
        }


        public override void Register(ContainerBuilder builder)
        {
            builder.RegisterAutoMocking(MockBehavior.Loose);

            builder.RegisterInstance(new StubStorageProvider(new ShellSettings { Name = ShellSettings.DefaultName })).As<IStorageProvider>();
            builder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>));
            builder.RegisterInstance(_clock).As<IClock>();
            builder.RegisterInstance(new Mock<UrlHelper>().Object).As<UrlHelper>();
            builder.RegisterType<StubCacheManager>().As<ICacheManager>();

            builder.Register(c =>
                {
                    var mock = new Mock<CombinatorResource>();
                    mock.SetupProperty(r => r.Content);
                    mock.SetupProperty(r => r.Type);
                    return mock.Object;
                }).As<CombinatorResource>();


            builder.RegisterType<CacheFileService>().As<ICacheFileService>();
        }

        public override void Init()
        {
            base.Init();

            _resourceRepository = new ResourceRepository(_container);
            _cacheFileService = _container.Resolve<ICacheFileService>();

            SaveTestResources();
        }

        
        [Test]
        public void SaveShouldBePersistent()
        {
            // Todo: adjust mocking that CombinatorResource's context can be filled and so the GetCombinedResources() method tested
            // if it returns the correct data
            
            var storageProvider = _container.Resolve<IStorageProvider>();
            Assert.That(storageProvider.GetFile(storageProvider.Combine("_PiedoneModules", storageProvider.Combine("Combinator", storageProvider.Combine("Styles", CacheFileService.ConvertFingerprintToStorageFormat(_cssResourcesFingerprint) + "-1.css")))), Is.Not.Null);

            var resources = _cacheFileService.GetCombinedResources(_jsResourceFingerprint, new CombinatorSettings());

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources.Count, Is.EqualTo(2));

            Assert.That(_cacheFileService.GetCount(), Is.EqualTo(3));

            Assert.That(_cacheFileService.Exists(_cssResourcesFingerprint, new CombinatorSettings()), Is.True);
        }

        [Test]
        public void EmtpyShouldDeleteAll()
        {
            _cacheFileService.Empty();
            ClearSession();

            Assert.That(_cacheFileService.GetCombinedResources(_cssResourcesFingerprint, new CombinatorSettings()).Count, Is.EqualTo(0));
            Assert.That(_cacheFileService.GetCombinedResources(_jsResourceFingerprint, new CombinatorSettings()).Count, Is.EqualTo(0));
            Assert.That(_cacheFileService.GetCount(), Is.EqualTo(0));
        }


        private void SaveTestResources()
        {
            var settings = new CombinatorSettings { ResourceBaseUri = new Uri("http://localhost") };

            var resource1 = _resourceRepository.NewResource(ResourceType.Style);
            resource1.Content = "test";
            _cacheFileService.Save(_cssResourcesFingerprint, resource1, settings);

            _cacheFileService.Save(_jsResourceFingerprint, _resourceRepository.NewResource(ResourceType.JavaScript), settings);
            _cacheFileService.Save(_jsResourceFingerprint, _resourceRepository.NewResource(ResourceType.JavaScript), settings);

            ClearSession();
        }


        private class StubStorageProvider : IStorageProvider
        {
            private FileSystemStorageProvider FileSystemStorageProvider { get; set; }
            public Func<string, IEnumerable<IStorageFolder>> ListFoldersPredicate { get; set; }
            public List<string> SavedStreams { get; set; }
            public List<string> CreatedFiles { get; set; }


            public StubStorageProvider(ShellSettings settings)
            {
                FileSystemStorageProvider = new FileSystemStorageProvider(settings);
                SavedStreams = new List<string>();
                CreatedFiles = new List<string>();
            }


            public string GetPublicUrl(string path)
            {
                return FileSystemStorageProvider.GetPublicUrl(path);
            }

            public IStorageFile GetFile(string path)
            {
                if (!FileExists(path)) return null;

                return new Mock<IStorageFile>().Object;
            }

            public IEnumerable<IStorageFile> ListFiles(string path)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IStorageFolder> ListFolders(string path)
            {
                return ListFoldersPredicate(path);
            }

            public bool TryCreateFolder(string path)
            {
                return false;
            }

            public void CreateFolder(string path)
            {
            }

            public void DeleteFolder(string path)
            {
            }

            public void RenameFolder(string path, string newPath)
            {
            }

            public void DeleteFile(string path)
            {
            }

            public void RenameFile(string path, string newPath)
            {
            }

            public IStorageFile CreateFile(string path)
            {
                CreatedFiles.Add(path);
                var mockFile = new Mock<IStorageFile>();
                mockFile.Setup(s => s.OpenWrite()).Returns(new MemoryStream());
                return mockFile.Object;
            }

            public string Combine(string path1, string path2)
            {
                return FileSystemStorageProvider.Combine(path1, path2);
            }

            public bool TrySaveStream(string path, Stream inputStream)
            {
                try { SaveStream(path, inputStream); }
                catch { return false; }

                return true;
            }

            public void SaveStream(string path, Stream inputStream)
            {
                SavedStreams.Add(path);
            }

            public bool FileExists(string path)
            {
                return CreatedFiles.Contains(path);
            }

            public string GetLocalPath(string url)
            {
                throw new NotImplementedException();
            }

            public bool FolderExists(string path)
            {
                return CreatedFiles.Any(p => p.StartsWith(path));
            }
            
            public string GetStoragePath(string url)
            {
                return FileSystemStorageProvider.GetStoragePath(url);
            }
            
            public void CopyFile(string originalPath, string duplicatePath)
            {
                FileSystemStorageProvider.CopyFile(originalPath, duplicatePath);
            }
        }


        private class StubStorageFolder : IStorageFolder
        {
            public string Path { get; set; }
            public string Name { get; set; }


            public StubStorageFolder(string name)
            {
                Name = name;
            }


            public string GetPath()
            {
                return Path;
            }

            public string GetName()
            {
                return Name;
            }

            public long GetSize()
            {
                return 0;
            }

            public DateTime GetLastUpdated()
            {
                return DateTime.Now;
            }

            public IStorageFolder GetParent()
            {
                return new StubStorageFolder("");
            }
        }
    }
}