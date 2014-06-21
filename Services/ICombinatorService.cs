using System.Collections.Generic;
using Orchard;
using Orchard.UI.Resources;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    /// <summary>
    /// Main service for processing stylesheets and scripts through Combinator.
    /// </summary>
    public interface ICombinatorService : IDependency
    {
        IList<ResourceRequiredContext> CombineStylesheets(IList<ResourceRequiredContext> resources, ICombinatorSettings settings);
        IList<ResourceRequiredContext> CombineScripts(IList<ResourceRequiredContext> resources, ICombinatorSettings settings);
    }
}
