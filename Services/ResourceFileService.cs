using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Orchard.FileSystems.VirtualPath;

namespace Piedone.Combinator.Services
{
    public class ResourceFileService : Piedone.Combinator.Services.IResourceFileService
    {
        private readonly IVirtualPathProvider _virtualPathProvider;

        public ResourceFileService(IVirtualPathProvider virtualPathProvider)
        {
            _virtualPathProvider = virtualPathProvider;
        }

        public string GetLocalResourceContent(string path)
        {
            // Maybe TryFileExists would be better?
            if (!_virtualPathProvider.FileExists(path)) throw new ApplicationException("Local resource file not found under " + path);

            string content;
            using (var stream = _virtualPathProvider.OpenFile(path))
            {
                content = new StreamReader(stream).ReadToEnd();
            }

            return content;
        }

        private string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
        private string ByteOrderMarkUtf8
        {
            get
            {
                if (_byteOrderMarkUtf8 == null) _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                return _byteOrderMarkUtf8;
            }
            set { _byteOrderMarkUtf8 = value; }
        }

        private UTF8Encoding _utf8Encoding;
        private UTF8Encoding Utf8Encoding
        {
            get
            {
                if (_utf8Encoding == null) _utf8Encoding = new UTF8Encoding(false);
                return _utf8Encoding;
            }
            set { _utf8Encoding = value; }
        }

        private WebClient _webClient;
        private WebClient WebClient
        {
            get
            {
                if (_webClient == null) _webClient = new WebClient();
                return _webClient;
            }
            set { _webClient = value; }
        }


        public string GetRemoteResourceContent(string url)
        {
            //var content = webClient.DownloadString(url);
            var content = Utf8Encoding.GetString(WebClient.DownloadData(url));
            if (content.StartsWith(_byteOrderMarkUtf8)) // Stripping "?"s from the beginning of css commments "/*"
            {
                content = content.Remove(0, _byteOrderMarkUtf8.Length);
            }



            return content;

            //resources.RemoveAt(resourceIndex);
        }
    }
}