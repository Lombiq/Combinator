using System.Text;
using System.Text.RegularExpressions;
using Autofac;
using Moq;
using NUnit.Framework;
using Orchard.Tests.Utility;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;
using Piedone.Combinator.Tests.Stubs;
using System;

namespace Piedone.Combinator.Tests.Services
{
    [TestFixture]
    public class ResourceProcessorServiceTests
    {
        private IContainer _container;
        private ResourceRepository _resourceRepository;
        private IResourceProcessingService _resourceProcessingService;

        [SetUp]
        public virtual void Init()
        {
            
            var builder = new ContainerBuilder();

            builder.RegisterAutoMocking(MockBehavior.Loose);

            builder.RegisterInstance(new StubMinificationService()).As<IMinificationService>();
            builder.RegisterType<ResourceProcessingService>().As<IResourceProcessingService>();

            _container = builder.Build();

            _resourceRepository = new ResourceRepository(_container);

            builder = new ContainerBuilder();
            builder.RegisterInstance(new StubResourceFileService(_resourceRepository)).As<IResourceFileService>();
            builder.Update(_container);

            _resourceProcessingService = _container.Resolve<IResourceProcessingService>();
        }

        [TestFixtureTearDown]
        public void Clean()
        {
        }

        [Test]
        public void MinificationExclusionWorks()
        {
            _resourceRepository.FillWithTestStyles();

            var settings = new CombinatorSettings
            {
                MinificationExcludeFilter = new Regex("test\\.css"), 
                MinifyResources = true
            };

            var resource = _resourceRepository.GetResource("~/Modules/Piedone.Combinator/Styles/test.css");
            _resourceProcessingService.ProcessResource(resource, new StringBuilder(), settings);
            Assert.That(resource.Content.Contains("minified: ~/Modules/Piedone.Combinator/Styles/test.css"), Is.False);

            resource = _resourceRepository.GetResource("~/Modules/Piedone.Combinator/Styles/test2.css");
            _resourceProcessingService.ProcessResource(resource, new StringBuilder(), settings);
            Assert.That(resource.Content.Contains("minified: ~/Modules/Piedone.Combinator/Styles/test2.css"), Is.True);
        }

        [Test]
        public void RelativeUrlsShouldBeAdjusted()
        {
            _resourceRepository.Clear();
            CombinatorResource resource;

            var type = ResourceType.Style;

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/urls.css", type);
            resource.Content = "background-image: url(\"/Images/Root.png\");\r\n";
            resource.Content += "background-image: url(Images/Sub.png);\r\n"; // Also changing quotes
            resource.Content += "background-image: url('Current.png');\r\n"; // Also changing quotes
            resource.Content += "background-image: url(\"../Images/Parent.png\");\r\n";
            resource.Content += "background-image: url(\"http://google.com/Images/Remote.png\");\r\n"; // This should remain intact

            _resourceProcessingService.ProcessResource(resource, new StringBuilder(), new CombinatorSettings());

            Assert.That(resource.Content.Contains("url(\"/Images/Root.png\");"), Is.True);
            Assert.That(resource.Content.Contains("url(\"/Modules/Piedone.Combinator/Styles/Images/Sub.png\");"), Is.True);
            Assert.That(resource.Content.Contains("url(\"/Modules/Piedone.Combinator/Styles/Current.png\");"), Is.True);
            Assert.That(resource.Content.Contains("url(\"/Modules/Piedone.Combinator/Images/Parent.png\");"), Is.True);
            Assert.That(resource.Content.Contains("url(\"//google.com/Images/Remote.png\");"), Is.True);
        }

        [Test]
        public void ImagesAreEmbedded()
        {
            Func<string, string> toBase64 =
                (url) =>
                    {
                        return Convert.ToBase64String(Encoding.Unicode.GetBytes(url));
                    };

            _resourceRepository.Clear();
            CombinatorResource resource;

            var type = ResourceType.Style;

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/imagese.css", type);
            resource.Content = "background-image: url(\"/Images/one.png\");\r\n";
            resource.Content += "background-image: url(\"/Images/two.png\");\r\n";
            resource.Content += "background-image: url(\"/Images/three.png\");\r\n";
            resource.Content += "background-image: url(\"http://google.com/Images/Remote.png\");\r\n";

            _resourceProcessingService.ProcessResource(resource, new StringBuilder(), new CombinatorSettings() { EmbedCssImages = true });

            Assert.That(resource.Content.Contains("url(\"data:image/png;base64," + toBase64("http://localhost/Images/one.png") + "\");"), Is.True);
            Assert.That(resource.Content.Contains("url(\"data:image/png;base64," + toBase64("http://localhost/Images/two.png") + "\");"), Is.True);
            Assert.That(resource.Content.Contains("url(\"data:image/png;base64," + toBase64("http://localhost/Images/three.png") + "\");"), Is.True);
            Assert.That(resource.Content.Contains("url(\"data:image/png;base64," + toBase64("http://google.com/Images/Remote.png") + "\");"), Is.True);
        }
    }
}