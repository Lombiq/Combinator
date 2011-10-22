using System;
using Orchard;

namespace Piedone.Combinator.Services
{
    public interface IResourceFileService : IDependency
    {
        string GetLocalResourceContent(string path);
        string GetRemoteResourceContent(string url);
    }
}
