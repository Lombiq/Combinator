using System;
using System.IO;
using System.Net;
using System.Text;
using Orchard;
using Orchard.FileSystems.VirtualPath;
using Orchard.Localization;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public class ResourceFileService : IResourceFileService
    {
        private readonly IVirtualPathProvider _virtualPathProvider;

        public Localizer T { get; set; }


        public ResourceFileService(
            IVirtualPathProvider virtualPathProvider)
        {
            _virtualPathProvider = virtualPathProvider;

            T = NullLocalizer.Instance;
        }


        public void LoadResourceContent(CombinatorResource resource)
        {
            if (resource.IsCdnResource || resource.IsRemoteStorageResource)
            {
                resource.Content = FetchRemoteResourceContent(resource);
            }
            else
            {
                resource.Content = FetchLocalResourceContent(resource);
            }
        }

        public byte[] GetImageContent(Uri imageUrl)
        {
            // Since these are public urls referenced in stylesheets, there's no simple way to tell their local path.
            // That's why all images are downloaded with WebClient.
            try
            {
                using (var wc = new WebClient())
                {
                    return wc.DownloadData(imageUrl);
                }
            }
            catch (WebException ex)
            {
                throw new ApplicationException("Downloading image content failed for " + imageUrl.ToString() + ".", ex);
            }
        }


        private string FetchLocalResourceContent(CombinatorResource resource)
        {
            var relativeVirtualPath = resource.RelativeVirtualPath;
            if (relativeVirtualPath == "~/") return "";
            // Maybe TryFileExists would be better?
            if (!_virtualPathProvider.FileExists(relativeVirtualPath)) throw new OrchardException(T("Local resource file not found under {0}", relativeVirtualPath));

            string content;
            using (var stream = _virtualPathProvider.OpenFile(relativeVirtualPath))
            {
                content = new StreamReader(stream).ReadToEnd();
            }

            return content;
        }

        private string FetchRemoteResourceContent(CombinatorResource resource)
        {
            using (var wc = new WebClient())
            {
                // This strips the UTF8 BOM automatically, see: http://stackoverflow.com/a/1317795/220230
                return Encoding.UTF8.GetString(wc.DownloadData(resource.AbsoluteUrl));
            }
        }
    }
}