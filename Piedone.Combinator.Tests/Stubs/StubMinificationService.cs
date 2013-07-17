using Piedone.Combinator.Services;

namespace Piedone.Combinator.Tests.Stubs
{
    class StubMinificationService : IMinificationService
    {
        public string MinifyCss(string css)
        {
            return "minified: " + css;
        }

        public string MinifyJavaScript(string javaScript)
        {
            return "minified: " + javaScript;
        }
    }
}
