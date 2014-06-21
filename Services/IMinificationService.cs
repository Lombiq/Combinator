using Orchard;

namespace Piedone.Combinator.Services
{
    /// <summary>
    /// Service for minifying static resources.
    /// </summary>
    public interface IMinificationService : IDependency
    {
        string MinifyCss(string css);
        string MinifyJavaScript(string javaScript);
    }
}
