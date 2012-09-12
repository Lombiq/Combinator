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

namespace Piedone.Combinator.Tests.Services
{
    [TestFixture]
    public class CacheFileServiceTests : DatabaseEnabledTestsBase
    {
        private ResourceRepository _resourceRepository;
        private ICacheFileService _cacheFileService;

        public override void Register(ContainerBuilder builder)
        {
            builder.RegisterAutoMocking(MockBehavior.Loose);

            builder.RegisterInstance(new StubStorageProvider(new ShellSettings { Name = ShellSettings.DefaultName })).As<IStorageProvider>();
            builder.RegisterGeneric(typeof(Repository<>)).As(typeof(IRepository<>));
            builder.RegisterInstance(_clock).As<IClock>();
            builder.RegisterType<StubCacheManager>().As<ICacheManager>();
            builder.RegisterType<Signals>().As<ISignals>();

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
        }


        protected override IEnumerable<Type> DatabaseTypes
        {
            get
            {
                return new[] {
                    typeof(CombinedFileRecord)
                };
            }
        }

        [Test]
        public void SaveShouldBePersistent()
        {
            // Todo: adjust mocking that CombinatorResource's context can be filled and so the GetCombinedResources() method tested
            // if it returns the correct data
            SaveTestResources();

            Assert.That(_container.Resolve<IStorageProvider>().GetFile("Combinator/Scripts/664456-1.js"), Is.Not.Null);

            var resources = _cacheFileService.GetCombinedResources(_cssResourcesHashCode);

            Assert.That(resources, Is.Not.Null);
            Assert.That(resources.Count, Is.EqualTo(2));

            Assert.That(_cacheFileService.GetCount(), Is.EqualTo(3));

            Assert.That(_cacheFileService.Exists(_cssResourcesHashCode), Is.True);
        }

        //[Test]
        //public void DeletionShouldDelete()
        //{
        //    SaveTestResources();

        //    _cacheFileService.Delete(_cssResourcesHashCode);
        //    ClearSession();

        //    Assert.That(_cacheFileService.GetCombinedResources(_cssResourcesHashCode).Count, Is.EqualTo(0));
        //    Assert.That(_cacheFileService.GetCount(), Is.EqualTo(1));
        //}

        [Test]
        public void EmtpyShouldDeleteAll()
        {
            SaveTestResources();

            _cacheFileService.Empty();
            ClearSession();

            Assert.That(_cacheFileService.GetCombinedResources(_cssResourcesHashCode).Count, Is.EqualTo(0));
            Assert.That(_cacheFileService.GetCombinedResources(_jsResourcesHashCode).Count, Is.EqualTo(0));
            Assert.That(_cacheFileService.GetCount(), Is.EqualTo(0));
        }


        private const int _jsResourcesHashCode = 664456;
        private const int _cssResourcesHashCode = 1151;

        private void SaveTestResources()
        {
            var resource1 = _resourceRepository.NewResource(ResourceType.JavaScript);
            resource1.Content = "test";
            _cacheFileService.Save(_jsResourcesHashCode, resource1);

            _cacheFileService.Save(_cssResourcesHashCode, _resourceRepository.NewResource(ResourceType.JavaScript));
            _cacheFileService.Save(_cssResourcesHashCode, _resourceRepository.NewResource(ResourceType.JavaScript));

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
                if (!CreatedFiles.Contains(path)) return null;

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