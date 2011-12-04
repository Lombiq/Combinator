using Orchard;

namespace Piedone.Combinator.Services
{
    public interface IResourceFileService : IDependency
    {
        string GetLocalResourceContent(string relativeVirtualPath);
        string GetRemoteResourceContent(string url);
    }
}
