using Orchard;
using System.Collections.Generic;
using Orchard.Logging;
using Orchard.UI.Resources;
using Orchard.Caching;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public interface ICombinatorService : IDependency
    {
        IList<ResourceRequiredContext> CombineStylesheets(IList<ResourceRequiredContext> resources, ICombinatorSettings settings);
        IList<ResourceRequiredContext> CombineScripts(IList<ResourceRequiredContext> resources, ICombinatorSettings settings);
    }
}
