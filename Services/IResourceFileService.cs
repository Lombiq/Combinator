using Orchard;
using System;

namespace Piedone.Combinator.Services
{
    public interface IResourceFileService : IDependency
    {
        string GetLocalResourceContent(string relativeVirtualPath);
        string GetRemoteResourceContent(Uri url);
    }
}
