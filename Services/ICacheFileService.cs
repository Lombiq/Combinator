using System;
using System.Collections.Generic;
using System.IO;
using Orchard;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public delegate void SpriteStreamWriter(Stream stream, string publicUrl);

    public interface ICacheFileService : IDependency
    {
        void Save(string fingerprint, CombinatorResource resource, Uri resourceBaseUri, bool useResourceShare);
        IList<CombinatorResource> GetCombinedResources(string fingerprint, bool useResourceShare);
        bool Exists(string fingerprint, bool useResourceShare);
        int GetCount();
        void Empty();
        void WriteSpriteStream(string fileName, SpriteStreamWriter streamWriter);
    }
}
