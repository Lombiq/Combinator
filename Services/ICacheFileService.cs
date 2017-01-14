using System;
using System.Collections.Generic;
using System.IO;
using Orchard;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public delegate void SpriteStreamWriter(Stream stream, string publicUrl);


    /// <summary>
    /// Service for managing static cache files, i.e. processed static resources.
    /// </summary>
    public interface ICacheFileService : IDependency
    {
        void Save(string fingerprint, CombinatorResource resource, ICombinatorSettings settings);
        IList<CombinatorResource> GetCombinedResources(string fingerprint, ICombinatorSettings settings);
        bool Exists(string fingerprint, ICombinatorSettings settings);
        int GetCount();
        void Empty();
        void WriteSpriteStream(string fileName, SpriteStreamWriter streamWriter);
    }
}
