using System.Collections.Generic;
using Orchard;
using Orchard.Caching;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public interface ICacheFileService : IDependency
    {
        void Save(int hashCode, ISmartResource resource);
        List<ISmartResource> GetCombinedResources(int hashCode);
        bool Exists(int hashCode);
        int GetCount();
        void Delete(int hashCode);
        void Empty();
        void MonitorCacheChangedSignal(AcquireContext<string> ctx, int hashCode);
    }
}
