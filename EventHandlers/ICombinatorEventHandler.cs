using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard.Events;

namespace Piedone.Combinator.EventHandlers
{
    public interface ICombinatorEventHandler : IEventHandler
    {
        void ConfigurationChanged();
        //void CombinationSaved(int hashCode);
        //void CombinationDeleted(int hashCode);
        void CacheEmptied();
        //void CacheChanged();
    }
}
