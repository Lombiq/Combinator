using System;
using Orchard;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public interface IResourceFileService : IDependency
    {
        void LoadResourceContent(CombinatorResource resource);
        byte[] GetImageContent(Uri imageUrl, int maxSizeKB);
    }
}
