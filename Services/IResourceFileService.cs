using System;
using Orchard;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public interface IResourceFileService : IDependency
    {
        string GetLocalResourceContent(CombinatorResource resource);
        string GetRemoteResourceContent(CombinatorResource resource);
        byte[] GetImageData(Uri imageUrl, int maxSizeKB);
    }
}
