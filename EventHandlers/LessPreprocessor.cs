using System.IO;
using dotless.Core;
using Orchard.Environment.Extensions;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.EventHandlers
{
    [OrchardFeature("Piedone.Combinator.Less")]
    public class LessPreprocessor : ICombinatorResourceEventHandler
    {
        public void OnContentLoaded(CombinatorResource resource)
        {
            if (Path.GetExtension(resource.AbsoluteUrl.ToString()).ToLowerInvariant() != ".less") return;

            resource.Content = Less.Parse(resource.Content);
        }

        public void OnContentProcessed(CombinatorResource resource)
        {
        }
    }
}