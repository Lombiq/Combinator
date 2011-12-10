using Orchard;
using Piedone.Combinator.Models;
using System;

namespace Piedone.Combinator.Services
{
    public interface IResourceFileService : IDependency
    {
        string GetLocalResourceContent(ISmartResource resource);
        string GetRemoteResourceContent(ISmartResource resource);
        string GetImageBase64Data(Uri imageUrl);
    }
}
