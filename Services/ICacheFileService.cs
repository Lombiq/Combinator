using System.Collections.Generic;
using Orchard;
using Piedone.Combinator.Helpers;
using Piedone.Combinator.Models;
using Orchard.Caching;

namespace Piedone.Combinator.Services
{
    public interface ICacheFileService : IDependency
    {
        void Save(int hashCode, ISmartResource resource);
        List<ISmartResource> GetCombinedResources(int hashCode);
        bool Exists(int hashCode);
        int GetCount();
        void Delete(int hashCode, ResourceType type);
        void Empty();
        void MonitorCacheChangedSignal(AcquireContext<string> ctx, int hashCode);
    }
}
