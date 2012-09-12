using System;
using System.IO;
using Autofac;
using Moq;
using NUnit.Framework;
using Orchard.FileSystems.VirtualPath;
using Orchard.Tests.Stubs;
using Orchard.Tests.Utility;
using Piedone.Combinator.Services;
using Piedone.Combinator.Tests.TestHelpers;

namespace Piedone.Combinator.Tests.Services
{
    [TestFixture]
    public class ResourceFileServiceTests
    {
        private IContainer _container;
        private ResourceRepository _resourceRepository;
        private IResourceFileService _resourceFileService;

        [SetUp]
        public virtual void Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAutoMocking(MockBehavior.Loose);


            builder.RegisterInstance(new StubVirtualPathProvider(new StubFileSystem(new StubClock()))).As<IVirtualPathProvider>();
            builder.RegisterType<ResourceFileService>().As<IResourceFileService>();

            CombinatorResourceHelper.Register(builder);

            _container = builder.Build();

            _resourceRepository = new ResourceRepository(_container);
            _resourceFileService = _container.Resolve<IResourceFileService>();
        }

        [TestFixtureTearDown]
        public void Clean()
        {
        }

        [Test]
        public void LocalResourcesAreRetrieved()
        {
            var path = "~/Modules/Piedone.Combinator/Styles/test.css";

            var virtualPathProvider = _container.Resolve<IVirtualPathProvider>();

            using (var stream = virtualPathProvider.CreateFile(path))
            {
                //var textData = Encoding.UTF8.GetBytes("testresource");
                //stream.Write(textData, 0, textData.Length);
                using (var sw = new StreamWriter(stream))
                {
                    sw.Write("testresource");
                }
            }

            var resource = _resourceRepository.NewResource(ResourceType.Style);
            resource.FillRequiredContext("LocalResourceTest", path);
            _resourceFileService.LoadResourceContent(resource);
            Assert.That(resource.Content, Is.EqualTo("testresource"));
        }

        [Test]
        public void RemoteResourcesAreRetrieved()
        {
            var resource = _resourceRepository.NewResource(ResourceType.JavaScript);
            resource.FillRequiredContext("RemoteResourceTest", "https://ajax.googleapis.com/ajax/libs/jquery/1.7.1/jquery.js");
            _resourceFileService.LoadResourceContent(resource);
            Assert.That(resource.Content.Length, Is.EqualTo(248235));
        }

        [Test]
        public void ImagesDataAreRetrieved()
        {
            var data = _resourceFileService.GetImageContent(new Uri("http://code.google.com/images/code_logo.gif"), 5);
            Assert.That(data.Length, Is.EqualTo(3383));
        }

        [Test]
        public void ImagesTooBigAreNotRetrieved()
        {
            var data = _resourceFileService.GetImageContent(new Uri("http://code.google.com/images/code_logo.gif"), 1);
            Assert.That(data, Is.Null);
        }
    }
}