using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Autofac;
using Moq;
using NUnit.Framework;
using Orchard.Caching;
using Orchard.Tests.Utility;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;
using Piedone.HelpfulLibraries.Tasks;

namespace Piedone.Combinator.Tests.Services
{
    [TestFixture]
    public class CombinatorServiceTests
    {
        private IContainer _container;
        private ResourceRepository _resourceRepository;
        private ICombinatorService _combinatorService;

        [SetUp]
        public virtual void Init()
        {
            var builder = new ContainerBuilder();

            builder.RegisterAutoMocking(MockBehavior.Loose);

            builder.RegisterType<StubCacheFileService>().As<ICacheFileService>();
            builder.RegisterType<Signals>().As<ISignals>();
            builder.RegisterInstance(new StubResourceProcessingService()).As<IResourceProcessingService>();
            builder.RegisterType<StubLockingCacheManager>().As<ILockingCacheManager>();

            builder.RegisterType<CombinatorService>().As<ICombinatorService>();

            _container = builder.Build();

            
            _resourceRepository = new ResourceRepository(_container);

            builder = new ContainerBuilder();
            builder.RegisterInstance(_resourceRepository).As<ResourceRepository>(); // For StubCacheFileService
            builder.Update(_container);

            _combinatorService = _container.Resolve<ICombinatorService>();
        }

        [TestFixtureTearDown]
        public void Clean()
        {
        }

        [Test]
        public void ConditionalStylesAreSplit()
        {
            _resourceRepository.Clear();
            CombinatorResource resource;

            var type = ResourceType.Style;

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/unconditional.css", type);

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/conditional.css", type);
            resource.RequiredContext.Settings.Condition = "gte IE 9";

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/conditional2.css", type);
            resource.RequiredContext.Settings.Condition = "gte IE 9";

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/conditional3.css", type);
            resource.RequiredContext.Settings.Condition = "IE 8";

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/unconditional2.css", type);

            var combinedResources = _combinatorService.CombineStylesheets(_resourceRepository.GetResources(type), new CombinatorSettings());

            Assert.That(combinedResources.Count, Is.EqualTo(4));
            Assert.That(combinedResources[0].Settings.Condition, Is.Empty);
            Assert.That(combinedResources[1].Settings.Condition, Is.EqualTo("gte IE 9"));
            Assert.That(combinedResources[2].Settings.Condition, Is.EqualTo("IE 8"));
            Assert.That(combinedResources[3].Settings.Condition, Is.Empty);
        }

        [Test]
        public void SetsAreSplit()
        {
            _resourceRepository.Clear();
            CombinatorResource resource;

            var type = ResourceType.Style;

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/1.css", type);
            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/2.css", type);
            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/3.css", type);
            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/4.css", type);
            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/5.css", type);
            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/6.css", type);

            var settings = new CombinatorSettings
            {
                ResourceSetFilters = new Regex[] { new Regex("1"), new Regex("4|5") }
            };

            var combinedResources = _combinatorService.CombineStylesheets(_resourceRepository.GetResources(type), settings);

            Assert.That(combinedResources.Count, Is.EqualTo(4));
        }

        [Test]
        public void CdnStylesAreCombined()
        {
            _resourceRepository.FillWithTestStyles();

            var combinedResources = _combinatorService.CombineStylesheets(_resourceRepository.GetResources(ResourceType.Style), new CombinatorSettings() { CombineCDNResources = true });

            Assert.That(combinedResources.Count, Is.EqualTo(1));
        }

        [Test]
        public void CdnStylesAreNotCombined()
        {
            _resourceRepository.FillWithTestStyles();

            var combinedResources = _combinatorService.CombineStylesheets(_resourceRepository.GetResources(ResourceType.Style), new CombinatorSettings() { CombineCDNResources = false });

            Assert.That(combinedResources.Count, Is.EqualTo(4));
            Assert.That(combinedResources[0].Resource.Name, Is.EqualTo("http://google.com/style.css")); // Url is a combined one, because of StubResourceProcessingService
            Assert.That(combinedResources[2].Resource.Name, Is.EqualTo("http://google.com/style2.css"));
        }

        [Test]
        public void ExcludedStylesAreNotCombined()
        {
            _resourceRepository.FillWithTestStyles(false);

            CombinatorResource resource;
            var type = ResourceType.Style;

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/test4.css", type);
            resource.LastUpdatedUtc = DateTime.UtcNow;
            resource.Content = "~/Modules/Piedone.Combinator/Styles/test4.css";

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/test5.css", type);
            resource.LastUpdatedUtc = DateTime.UtcNow;
            resource.Content = "~/Modules/Piedone.Combinator/Styles/test5.css";

            var combinedResources = _combinatorService.CombineStylesheets(_resourceRepository.GetResources(type), new CombinatorSettings { CombinationExcludeFilter = new Regex("test2\\.css|test5\\.css") });

            Assert.That(combinedResources.Count, Is.EqualTo(4));
            Assert.That(combinedResources[1].Resource.Url, Is.StringStarting("/Modules/Piedone.Combinator/Styles/test2.css"));
            Assert.That(combinedResources[3].Resource.Url, Is.StringStarting("/Modules/Piedone.Combinator/Styles/test5.css"));
        }

        [Test]
        public void SpriteGenerationRuns()
        {
            _resourceRepository.Clear();
            CombinatorResource resource;

            var type = ResourceType.Style;

            resource = _resourceRepository.SaveResource("~/Modules/Piedone.Combinator/Styles/1.css", type);

            var settings = new CombinatorSettings
            {
                GenerateImageSprites = true
            };

            var combinedResources = _combinatorService.CombineStylesheets(_resourceRepository.GetResources(type), settings);
            resource = _resourceRepository.GetResource(combinedResources.First());

            Assert.That(resource.Content.Contains("ImageSprite"), Is.True);
        }



        private class StubResourceProcessingService : IResourceProcessingService
        {
            public void ProcessResource(CombinatorResource resource, StringBuilder combinedContent, ICombinatorSettings settings)
            {
                resource.Content = "processed: " + resource.Content;
                combinedContent.Append(resource.Content);
            }

            public void ReplaceCssImagesWithSprite(CombinatorResource resource)
            {
                resource.Content += "ImageSprite";
            }
        }

        private class StubCacheFileService : ICacheFileService
        {
            private Dictionary<int, int> sliceCounts = new Dictionary<int, int>();
            private readonly ISignals _signals;
            private readonly ResourceRepository _resourceRepository;

            public StubCacheFileService(ISignals signals, ResourceRepository resourceRepository)
            {
                _signals = signals;
                _resourceRepository = resourceRepository;
            }

            public void Save(int hashCode, CombinatorResource resource)
            {
                int count;
                sliceCounts.TryGetValue(hashCode, out count);
                sliceCounts[hashCode] = ++count;

                var sliceName = hashCode + "-" + count;

                if (!resource.IsOriginal)
                {
                    var url = "http://localhost/Media/Combinator/";
                    if (resource.Type == ResourceType.Style) url += "CombinedStyles/" + sliceName + ".css";
                    else if (resource.Type == ResourceType.JavaScript) url += "CombinedScripts/" + sliceName + ".js";

                    resource.RequiredContext.Resource.SetUrl(url);
                }

                resource.LastUpdatedUtc = DateTime.UtcNow;

                _resourceRepository.SaveResource(sliceName, resource);
            }

            public IList<CombinatorResource> GetCombinedResources(int hashCode)
            {
                return (from r in _resourceRepository.Resources 
                        where r.Key.Contains(hashCode.ToString() + "-")
                        select r.Value).ToList();
            }

            public bool Exists(int hashCode)
            {
                return false;
            }

            public int GetCount()
            {
                throw new NotImplementedException();
            }

            public void Delete(int hashCode)
            {
                throw new NotImplementedException();
            }

            public void Empty()
            {
                throw new NotImplementedException();
            }

            public void MonitorCacheChangedSignal(AcquireContext<string> ctx, int hashCode)
            {
                // Immediately invalidated the cache
                ctx.Monitor(_signals.When("CombinatorSignal"));
                _signals.Trigger("CombinatorSignal");
            }

            public void WriteSpriteStream(string fileName, SpriteStreamWriter streamWriter)
            {
                throw new NotImplementedException();
            }
        }

        private class StubLockingCacheManager : ILockingCacheManager
        {
            public TResult Get<TResult>(string key, Func<AcquireContext<string>, TResult> acquire, Func<TResult> fallback, int millisecondsTimeout = 4000)
            {
                return acquire(new AcquireContext<string>(key, null));
            }
        }

        //public class StubSmartResource : CombinatorResource
        //{
        //    public Uri PublicUrl { get; set; }

        //    public string RelativeVirtualPath
        //    {
        //        get { throw new NotImplementedException(); }
        //    }

        //    public ResourceRequiredContext RequiredContext { get; set; }
        //    public ResourceDefinition Resource
        //    {
        //        get { return RequiredContext.Resource; }
        //        set { RequiredContext.Resource = value; }
        //    }
        //    public RequireSettings Settings
        //    {
        //        get { return RequiredContext.Settings; }
        //        set { RequiredContext.Settings = value; }
        //    }

        //    public bool IsCDNResource { get; set; }
        //    public bool IsConditional { get; set; }
        //    public Uri UrlOverride { get; set; }
        //    public Helpers.ResourceType Type { get; set; }
        //    public string Content { get; set; }

        //    public void FillRequiredContext(string url, Helpers.ResourceType resourceType, string serializedSettings = "")
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public void FillRequiredContext(string name, string url, Helpers.ResourceType resourceType, string serializedSettings = "")
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public bool SettingsEqual(CombinatorResource other)
        //    {
        //    }

        //    public string GetSerializedSettings()
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public void FillSettingsFromSerialization(string settings)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}
    }
}