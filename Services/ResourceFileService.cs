using System;
using System.IO;
using System.Net;
using System.Text;
using Orchard;
using Orchard.FileSystems.VirtualPath;

namespace Piedone.Combinator.Services
{
    public class ResourceFileService : IResourceFileService
    {
        private readonly IVirtualPathProvider _virtualPathProvider;
        private readonly WorkContext _workContext;

        public ResourceFileService(
            IVirtualPathProvider virtualPathProvider,
            WorkContext workContext)
        {
            _virtualPathProvider = virtualPathProvider;
            _workContext = workContext;
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

        #region Path handling
        private string _applicationPath;
        private string ApplicationPath
        {
            get
            {
                if (_applicationPath == null) _applicationPath = _workContext.HttpContext.Request.ApplicationPath;
                return _applicationPath;
            }
            set { _applicationPath = value; }
        }

        public string GetPublicRelativeUrl(string relativeVirtualPath)
        {
            relativeVirtualPath = relativeVirtualPath.Remove(0, 1);
            return (ApplicationPath != "/") ? ApplicationPath + relativeVirtualPath : relativeVirtualPath;
        }

        public string GetRelativeVirtualPath(string fullPath)
        {
            if (fullPath.StartsWith(ApplicationPath))
            {
                // Strips e.g. /OrchardLocal
                if (ApplicationPath != "/")
                {
                    int place = fullPath.IndexOf(ApplicationPath);
                    // Finds the first occurence and replaces it with empty string
                    fullPath = fullPath.Remove(place, ApplicationPath.Length).Insert(place, "");
                }

                fullPath = "~" + fullPath;
            }

            return fullPath;
        }
        #endregion

        #region Remote resource handling
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
        }
        #endregion
    }
}