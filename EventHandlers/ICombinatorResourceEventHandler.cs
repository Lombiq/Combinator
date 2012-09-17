using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard.Events;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.EventHandlers
{
    public interface ICombinatorResourceEventHandler : IEventHandler
    {
        void OnContentLoaded(CombinatorResource resource);
        void OnContentProcessed(CombinatorResource resource);
    }
}
