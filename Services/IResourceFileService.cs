using System;
using Orchard;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    /// <summary>
    /// Service for dealing with resource files.
    /// </summary>
    public interface IResourceFileService : IDependency
    {
        void LoadResourceContent(CombinatorResource resource);
        byte[] GetImageContent(Uri imageUrl);
    }
}