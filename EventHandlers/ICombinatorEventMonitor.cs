using Orchard;
using Orchard.Caching;

namespace Piedone.Combinator.EventHandlers
{
    public interface ICombinatorEventMonitor : IDependency
    {
        void MonitorConfigurationChanged(IAcquireContext acquireContext);
        void MonitorCacheEmptied(IAcquireContext acquireContext);
        void MonitorBundleChanged(IAcquireContext acquireContext, int hashCode);
    }
}
