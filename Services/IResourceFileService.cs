using System;
using Orchard;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public interface IResourceFileService : IDependency
    {
        string GetLocalResourceContent(ISmartResource resource);
        string GetRemoteResourceContent(ISmartResource resource);
        string GetImageBase64Data(Uri imageUrl, int maxSizeKB);
    }
}
