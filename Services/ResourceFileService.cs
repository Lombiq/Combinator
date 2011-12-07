using System;
using System.IO;
using System.Net;
using System.Text;
using Orchard.FileSystems.VirtualPath;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public class ResourceFileService : IResourceFileService
    {
        private readonly IVirtualPathProvider _virtualPathProvider;

        public ResourceFileService(
            IVirtualPathProvider virtualPathProvider)
        {
            _virtualPathProvider = virtualPathProvider;
        }


        public string GetLocalResourceContent(ISmartResource resource)
        {
            var relativeVirtualPath = resource.RelativeVirtualPath;

            // Maybe TryFileExists would be better?
            if (!_virtualPathProvider.FileExists(relativeVirtualPath)) throw new ApplicationException("Local resource file not found under " + relativeVirtualPath);

            string content;
            using (var stream = _virtualPathProvider.OpenFile(relativeVirtualPath))
            {
                content = new StreamReader(stream).ReadToEnd();
            }

            return content;
        }

        public string GetRemoteResourceContent(ISmartResource resource)
        {
            using (var wc = new WebClient())
            {
                var byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                var content = new UTF8Encoding(false).GetString(wc.DownloadData(resource.PublicUrl));
                if (content.StartsWith(byteOrderMarkUtf8)) // Stripping "?"s from the beginning of css commments "/*"
                {
                    content = content.Remove(0, byteOrderMarkUtf8.Length);
                }
                return content; 
            }
        }
    }
}