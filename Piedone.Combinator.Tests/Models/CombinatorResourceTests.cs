using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.ComponentModel;
using Autofac;
using Moq;
using Piedone.Combinator.Models;
using Orchard.Tests.Stubs;

namespace Piedone.Combinator.Tests.Models
{
    [TestFixture]
    public class CombinatorResourceTests
    {
        private ResourceRepository _resourceRepository;

        [SetUp]
        public virtual void Init()
        {
            _resourceRepository = new ResourceRepository(new ContainerBuilder().Build());
        }

        [TestFixtureTearDown]
        public void Clean()
        {
        }

        [Test]
        public void LocalResourcesAreIdentified()
        {
            var resource = _resourceRepository.NewResource(ResourceType.Style);
            resource.FillRequiredContext("LocalResourceTest", "~/Modules/Piedone.Combinator/Styles/test.css");

            Assert.That(resource.IsCdnResource, Is.False);
        }

        [Test]
        public void CdnResourcesAreIdentified()
        {
            var resource = _resourceRepository.NewResource(ResourceType.Style);
            resource.FillRequiredContext("RemoteResourceTest", "https://ajax.googleapis.com/ajax/libs/jquery/1.7.1/jquery.js");

            Assert.That(resource.IsCdnResource, Is.True);
        }

        //private CombinatorResource Factory(ResourceType type)
        //{
        //    return new CombinatorResource(type, new StubHttpContextAccessor() { StubContext = new StubHttpContext() });
        //}
    }
}
