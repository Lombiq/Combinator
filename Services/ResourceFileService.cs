using System;
using System.IO;
using System.Net;
using System.Text;
using Orchard.FileSystems.VirtualPath;

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

        
        public string GetLocalResourceContent(string relativeVirtualPath)
        {
            // Maybe TryFileExists would be better?
            if (!_virtualPathProvider.FileExists(relativeVirtualPath)) throw new ApplicationException("Local resource file not found under " + relativeVirtualPath);

            string content;
            using (var stream = _virtualPathProvider.OpenFile(relativeVirtualPath))
            {
                content = new StreamReader(stream).ReadToEnd();
            }

            return content;
        }

        public string GetRemoteResourceContent(Uri url)
        {
            var byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            //var content = webClient.DownloadString(url);
            var content = new UTF8Encoding(false).GetString(new WebClient().DownloadData(url));
            if (content.StartsWith(byteOrderMarkUtf8)) // Stripping "?"s from the beginning of css commments "/*"
            {
                content = content.Remove(0, byteOrderMarkUtf8.Length);
            }

            return content;
        }
    }
}