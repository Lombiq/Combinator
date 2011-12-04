using System.Collections.Generic;
using Orchard;
using Piedone.Combinator.Helpers;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public interface ICacheFileService : IDependency
    {
        string Save(int hashCode, ISmartResource resource);
        List<ISmartResource> GetCombinedResources(int hashCode);
        bool Exists(int hashCode);
        int GetCount();
        void Delete(int hashCode, ResourceType type);
        void Empty();
    }
}
