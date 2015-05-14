using System;
using System.Web;
using Autofac;
using Orchard.Mvc;
using Orchard.Services;
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
            builder.RegisterInstance(new StubHttpContextAccessor(new StubHttpContext())).As<IHttpContextAccessor>();
            builder.RegisterType<StubJsonConverter>().As<IJsonConverter>();
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

        private class StubJsonConverter : IJsonConverter
        {
            public string Serialize(object o)
            {
                throw new NotImplementedException();
            }

            public string Serialize(object o, JsonFormat format)
            {
                throw new NotImplementedException();
            }

            public dynamic Deserialize(string json)
            {
                throw new NotImplementedException();
            }

            public T Deserialize<T>(string json)
            {
                throw new NotImplementedException();
            }
        }
    }
}
