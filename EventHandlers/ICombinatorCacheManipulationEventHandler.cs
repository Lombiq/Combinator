using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orchard.Events;

namespace Piedone.Combinator.EventHandlers
{
    /// <summary>
    /// Through this event handler 3rd party modules can manipulate the cache without having a dependendency and a hard reference
    /// on Combinator.
    /// </summary>
    public interface ICombinatorCacheManipulationEventHandler : IEventHandler
    {
        void EmptyCache();
    }
}
