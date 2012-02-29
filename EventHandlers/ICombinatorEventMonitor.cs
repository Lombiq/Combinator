using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard.Caching;

namespace Piedone.Combinator.EventHandlers
{
    public interface ICombinatorEventMonitor : ICombinatorEventHandler
    {
        void MonitorConfigurationChanged(IAcquireContext acquireContext);
        void MonitorCacheChanged(IAcquireContext acquireContext);
    }
}
