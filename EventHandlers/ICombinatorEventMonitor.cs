using Orchard;
using Orchard.Caching;

namespace Piedone.Combinator.EventHandlers
{
    public interface ICombinatorEventMonitor : IDependency
    {
        void MonitorConfigurationChanged(string cacheKey);
        void MonitorCacheEmptied(string cacheKey);
        void MonitorBundleChanged(string cacheKey, int hashCode);
    }
}
