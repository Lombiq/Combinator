using System;
using System.IO;
using System.Text;
using Orchard.UI.Resources;
using Piedone.HelpfulLibraries.Utilities;

namespace Piedone.Combinator.Extensions
{
    public static class ResourceDefinitionExtensions
    {
        /// <summary>
        /// Gets the ultimate full path of a resource, even if it uses CDN. Note that the paths are not uniform, they're
        /// concatenated from the resoure's paths, therefore they can well be virtual relative paths (starting with a tilde)
        /// or relative public urls.
        /// </summary>
        public static string GetFullPath(this ResourceDefinition resource)
        {
            if (string.IsNullOrEmpty(resource.Url)) return resource.UrlCdn;

            if (resource.Url.Contains("~")) return resource.Url;

            return Path.Combine(resource.BasePath, resource.Url);
        }

        public static void SetUrlProtocolRelative(this ResourceDefinition resource, Uri url)
        {
            resource.SetUrl(url.ToStringWithoutScheme());
        }

        public static bool TagAttributesEqual(this ResourceDefinition resource, ResourceDefinition other)
        {
            return resource.StringifyAttributes() == other.StringifyAttributes();
        }


        private static string StringifyAttributes(this ResourceDefinition resource)
        {
            if (resource.TagBuilder.Attributes.Count == 0) return "";

            var sb = new StringBuilder();
            foreach (var item in resource.TagBuilder.Attributes)
            {
                sb.Append(item.Key + "-" + item.Value);
            }

            return sb.ToString();
        }
    }
}