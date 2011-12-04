using Orchard;
using System.Collections.Generic;
using Orchard.Logging;
using Orchard.UI.Resources;
namespace Piedone.Combinator.Services
{
    public interface ICombinatorService : IDependency
    {
        IList<ResourceRequiredContext> CombineScripts(IList<ResourceRequiredContext> resources, bool combineCDNResources = false, bool minifyResources = true, string minificationExcludeRegex = "");
        IList<ResourceRequiredContext> CombineStylesheets(IList<ResourceRequiredContext> resources, bool combineCDNResources = false, bool minifyResources = true, string minificationExcludeRegex = "");
        ILogger Logger { get; set; }
    }
}
