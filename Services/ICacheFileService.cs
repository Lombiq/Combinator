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
        void Save(int hashCode, CombinatorResource resource, Uri resourceBaseUri, bool useResourceShare);
        IList<CombinatorResource> GetCombinedResources(int hashCode, bool useResourceShare);
        bool Exists(int hashCode, bool useResourceShare);
        int GetCount();
        void Empty();
        void WriteSpriteStream(string fileName, SpriteStreamWriter streamWriter);
    }
}
