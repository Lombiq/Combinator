using System;
using Orchard;

namespace Piedone.Combinator.Services
{
    public interface IMinificationService : ISingletonDependency
    {
        string MinifyCss(string css);
        string MinifyJavaScript(string javaScript);
    }
}
