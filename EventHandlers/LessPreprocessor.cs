using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Piedone.Combinator.Models;
using Orchard.Environment.Extensions;
using System.IO;
using dotless.Core;

namespace Piedone.Combinator.EventHandlers
{
    [OrchardFeature("Piedone.Combinator.Less")]
    public class LessPreprocessor : ICombinatorResourceEventHandler
    {
        public void OnContentLoaded(CombinatorResource resource)
        {
            if (Path.GetExtension(resource.RelativeUrl.ToString()).ToLowerInvariant() != ".less") return;

            resource.Content = Less.Parse(resource.Content);
        }

        public void OnContentProcessed(CombinatorResource resource)
        {
        }
    }
}