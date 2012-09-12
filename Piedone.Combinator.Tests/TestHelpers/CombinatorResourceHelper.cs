using System;
using System.Web;
using Autofac;
using Orchard.Mvc;
using Orchard.Tests.Stubs;
using Piedone.Combinator.Services;
using Piedone.HelpfulLibraries.Serialization;

namespace Piedone.Combinator.Tests.TestHelpers
{
    static class CombinatorResourceHelper
    {
        public static void Register(ContainerBuilder builder)
        {
            // CombinatorResource and CombinatorResourceManager are not mocked currently. Although this is generally bad, for proper mocking a big part of
            // the class would have to be replicated here...
            builder.RegisterInstance(new StubHttpContextAccessor() { StubContext = new StubHttpContext() }).As<IHttpContextAccessor>();
            builder.RegisterType<SimpleSerializer>().As<ISimpleSerializer>();
            builder.RegisterType<CombinatorResourceManager>().As<ICombinatorResourceManager>();
        }

        private class StubHttpContext : HttpContextBase
        {
            public override HttpRequestBase Request
            {
                get { return new StubHttpRequest(); }
            }
        }

        private class StubHttpRequest : HttpRequestBase
        {
            public override Uri Url
            {
                get { return new Uri("http://localhost"); }
            }

            public override string ApplicationPath
            {
                get { return "/"; }
            }
        }
    }
}
