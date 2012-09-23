using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Environment.Extensions;
using Piedone.Combinator.Models;
using System.IO;
using SassAndCoffee.Ruby.Sass;
using Orchard.FileSystems.VirtualPath;

namespace Piedone.Combinator.EventHandlers
{
    [OrchardFeature("Piedone.Combinator.Sass")]
    public class SassPreprocessor : ICombinatorResourceEventHandler
    {
        private readonly IVirtualPathProvider _virtualPathProvider;

        public SassPreprocessor(IVirtualPathProvider virtualPathProvider)
        {
            _virtualPathProvider = virtualPathProvider;
        }

        public void OnContentLoaded(CombinatorResource resource)
        {
            var extension = Path.GetExtension(resource.RelativeUrl.ToString()).ToLowerInvariant();
            if (extension != ".sass" && extension != ".scss") return;

            using (var compiler = new SassCompiler())
            {
                resource.Content = compiler.Compile(_virtualPathProvider.MapPath(resource.RelativeVirtualPath).Replace("\\", @"\"), false, new List<string>());
            }
        }

        public void OnContentProcessed(CombinatorResource resource)
        {
        }
    }
}