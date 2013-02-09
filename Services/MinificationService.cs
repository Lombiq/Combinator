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
            if (string.IsNullOrEmpty(css)) return string.Empty;

            return new CssCompressor().Compress(css);
        }

        public string MinifyJavaScript(string javaScript)
        {
            if (string.IsNullOrEmpty(javaScript)) return string.Empty;

            return new JavaScriptCompressor().Compress(javaScript);
        }
    }
}