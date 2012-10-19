using System.Collections.Generic;
using System.IO;
using Orchard.Environment.Extensions;
using Orchard.FileSystems.VirtualPath;
using Piedone.Combinator.Models;
using SassAndCoffee.Ruby.Sass;

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