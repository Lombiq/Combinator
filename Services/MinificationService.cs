using Yahoo.Yui.Compressor;

namespace Piedone.Combinator.Services
{
    /// <summary>
    /// Wraps YUI compressor
    /// </summary>
    public class MinificationService : IMinificationService
    {
        public string MinifyCss(string css)
        {
            return new CssCompressor().Compress(css);
        }

        public string MinifyJavaScript(string javaScript)
        {
            return new JavaScriptCompressor().Compress(javaScript);
        }
    }
}