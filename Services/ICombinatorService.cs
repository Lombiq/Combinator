using Orchard;
namespace Piedone.Combinator.Services
{
    public interface ICombinatorService : IDependency
    {
        System.Collections.Generic.IList<Orchard.UI.Resources.ResourceRequiredContext> CombineScripts(System.Collections.Generic.IList<Orchard.UI.Resources.ResourceRequiredContext> resources, bool combineCDNResources = false, bool minifyResources = true, string minificationExcludeRegex = "");
        System.Collections.Generic.IList<Orchard.UI.Resources.ResourceRequiredContext> CombineStylesheets(System.Collections.Generic.IList<Orchard.UI.Resources.ResourceRequiredContext> resources, bool combineCDNResources = false, bool minifyResources = true, string minificationExcludeRegex = "");
        Orchard.Logging.ILogger Logger { get; set; }
        Orchard.UI.Resources.IResourceManager ResourceManager { get; set; }
    }
}
