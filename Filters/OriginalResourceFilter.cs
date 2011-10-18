using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Environment.Extensions;
using Orchard.Mvc.Filters;
using System.Web.Mvc;
using Orchard.UI.Resources;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace Piedone.Combinator.Filters
{
    [OrchardFeature("Piedone.Combinator")]
    public class OriginalResourceFilter : FilterProvider, IActionFilter
    {
        private readonly IResourceManager _resourceManager;

        public OriginalResourceFilter(
            IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Response.Filter != null)
                filterContext.HttpContext.Response.Filter = new StylesheetFilter(
                    filterContext.HttpContext.Response.Filter,
                    filterContext.HttpContext.Response.Output.Encoding,
                    _resourceManager);
        }
    }

    /// <summary>
    /// Base taken from OfficineK.HTMLToolkit
    /// </summary>
    internal class StylesheetFilter : Stream
    {
        private readonly Stream _sink;
        private Encoding _encoding;
        private readonly IResourceManager _resourceManager;

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { return 0; } }
        public override long Position { get; set; }

        public Encoding Encoding { get { return _encoding; } set { _encoding = value; } }

        public StylesheetFilter(Stream sink, Encoding encoding, IResourceManager resourceManager)
        {
            _sink = sink;
            _encoding = encoding;
            _resourceManager = resourceManager;
        }

        public override void Flush()
        {
            _sink.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _sink.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _sink.SetLength(value);
        }

        public override void Close()
        {
            _sink.Close();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _sink.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var data = new byte[count];
            Buffer.BlockCopy(buffer, offset, data, 0, count);
            string html = _encoding.GetString(buffer);

            var styleRegex = new Regex("<link.*href=('|\")(.*\\.css)('|\").*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = styleRegex.Matches(html);
            var stylesheetUrls = new List<string>(matches.Count);
            foreach (Match match in matches)
            {
                string url = match.Groups[2].Value;
                if (!string.IsNullOrEmpty(url.Trim()))
                {
                    stylesheetUrls.Add(url);
                }
            }

            if (stylesheetUrls.Count != 0) ((CombinedResourceManager)_resourceManager).CombineStylesheets(stylesheetUrls);

            _sink.Write(data, 0, data.GetLength(0));

            //byte[] outdata = _encoding.GetBytes(html.Trim());
            //_sink.Write(outdata, 0, outdata.GetLength(0));
        }
    }
}