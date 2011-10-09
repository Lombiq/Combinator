using System.Collections.Generic;
using Orchard;
using Piedone.Combinator.Helpers;

namespace Piedone.Combinator.Services
{
    public interface ICacheFileService : IDependency
    {
        string Save(int hashCode, ResourceType type, string content);
        List<string> GetPublicUrls(int hashCode);
        bool Exists(int hashCode);
        int GetCount();
        void Delete(int hashCode, ResourceType type);
        void Empty();
    }
}
