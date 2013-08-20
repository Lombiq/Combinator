using Orchard.Caching;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.EventHandlers
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorEventMonitor : ICombinatorEventMonitor, ICombinatorEventHandler
    {
        private readonly ISignals _signals;

        private const string ConfigurationChangedSignal = "Piedone.Combinator.ConfigurationChangedSignal";
        private const string CacheEmptiedSignal = "Piedone.Combinator.CacheEmptiedSignal";
        private const string BundleChangedSignal = "Piedone.Combinator.BundleChangedSignal.";


        public CombinatorEventMonitor(ISignals signals)
        {
            _signals = signals;
        }


        public void MonitorConfigurationChanged(IAcquireContext acquireContext)
        {
            acquireContext.Monitor(_signals.When(ConfigurationChangedSignal));
        }

        public void MonitorCacheEmptied(IAcquireContext acquireContext)
        {
            acquireContext.Monitor(_signals.When(CacheEmptiedSignal));
        }

        public void MonitorBundleChanged(IAcquireContext acquireContext, int hashCode)
        {
            acquireContext.Monitor(_signals.When(BundleChangedSignal + hashCode.ToString()));
        }

        public void ConfigurationChanged()
        {
            _signals.Trigger(ConfigurationChangedSignal);
        }

        public void CacheEmptied()
        {
            _signals.Trigger(CacheEmptiedSignal);
        }

        public void BundleChanged(int hashCode)
        {
            _signals.Trigger(BundleChangedSignal + hashCode.ToString());
        }
    }
}