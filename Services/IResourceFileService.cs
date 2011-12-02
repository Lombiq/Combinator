using Orchard;

namespace Piedone.Combinator.Services
{
    public interface IResourceFileService : IDependency
    {
        string GetLocalResourceContent(string relativeVirtualPath);
        string GetPublicRelativeUrl(string relativeVirtualPath);
        string GetRelativeVirtualPath(string fullPath);
        string GetRemoteResourceContent(string url);
    }
}
