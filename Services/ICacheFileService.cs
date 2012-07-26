using System.Collections.Generic;
using Orchard;
using Piedone.Combinator.Models;
using System.IO;

namespace Piedone.Combinator.Services
{
    public delegate void SpriteStreamWriter(Stream stream, string publicUrl);

    public interface ICacheFileService : IDependency
    {
        void Save(int hashCode, CombinatorResource resource);
        IList<CombinatorResource> GetCombinedResources(int hashCode);
        bool Exists(int hashCode);
        int GetCount();
        //void Delete(int hashCode);
        void Empty();
        void WriteSpriteStream(string fileName, SpriteStreamWriter streamWriter);
    }
}
