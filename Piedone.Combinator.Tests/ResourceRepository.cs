using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Orchard.UI.Resources;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;
using Piedone.Combinator.Tests.TestHelpers;

namespace Piedone.Combinator.Tests
{
    class ResourceRepository
    {
        private readonly IContainer _container;

        public Dictionary<string, CombinatorResource> Resources { get; set; }

        public ResourceRepository(IContainer container)
        {
            _container = container;

            var builder = new ContainerBuilder();
            CombinatorResourceHelper.Register(builder);
            builder.Update(_container);

            Resources = new Dictionary<string, CombinatorResource>();
        }

        public CombinatorResource NewResource(ResourceType resourceType)
        {
            return _container.Resolve<ICombinatorResourceManager>().ResourceFactory(resourceType);
        }

        public CombinatorResource SaveResource(string url, ResourceType resourceType)
        {
            return SaveResource(url, url, resourceType);
        }

        public CombinatorResource SaveResource(string name, string url, ResourceType resourceType)
        {
            var resource = NewResource(resourceType);

            resource.FillRequiredContext(name, url);
            resource.Content = "";

            SaveResource(resource);

            return resource;
        }

        public void SaveResource(CombinatorResource resource)
        {
            Resources[resource.RequiredContext.Resource.Name] = resource;
        }

        public void SaveResource(string key, CombinatorResource resource)
        {
            Resources[key] = resource;
        }

        public void FillWithTestStyles(bool includeCDNResources = true)
        {
            Clear();
            CombinatorResource resource;

            var type = ResourceType.Style;

            if (includeCDNResources)
            {
                resource = SaveResource("http://google.com/style.css", type);
                resource.Content = "http://google.com/style.css";
            }

            resource = SaveResource("~/Modules/Piedone.Combinator/Styles/test.css", type);
            resource.LastUpdatedUtc = DateTime.UtcNow;
            resource.Content = "~/Modules/Piedone.Combinator/Styles/test.css";

            if (includeCDNResources)
            {
                resource = SaveResource("http://google.com/style2.css", type);
                resource.LastUpdatedUtc = DateTime.UtcNow;
                resource.Content = "http://google.com/style2.css";
            }

            resource = SaveResource("~/Modules/Piedone.Combinator/Styles/test2.css", type);
            resource.LastUpdatedUtc = DateTime.UtcNow;
            resource.Content = "~/Modules/Piedone.Combinator/Styles/test2.css";

            resource = SaveResource("~/Modules/Piedone.Combinator/Styles/test3.css", type);
            resource.LastUpdatedUtc = DateTime.UtcNow;
            resource.Content = "~/Modules/Piedone.Combinator/Styles/test3.css";
        }

        public CombinatorResource GetResource(CombinatorResource resource)
        {
            return GetResource(resource.RequiredContext.Resource.Name);
        }

        public CombinatorResource GetResource(string name)
        {
            return Resources[name];
        }

        public CombinatorResource GetResource(ResourceRequiredContext resource)
        {
            return (from r in Resources
                    where r.Value.RequiredContext.Resource.Url == resource.Resource.Url
                    select r.Value).First();
        }

        public IList<ResourceRequiredContext> GetResources(ResourceType type)
        {
            return Resources.Where(p => p.Value.Type == ResourceType.Style).Select(p => p.Value.RequiredContext).ToList();
        }

        public void Clear()
        {
            Resources.Clear();
        }
    }
}
