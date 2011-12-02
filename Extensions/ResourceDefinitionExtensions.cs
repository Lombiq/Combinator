using System;
using Orchard.Environment.Extensions;
using Orchard.UI.Resources;
using System.IO;

namespace Piedone.Combinator.Extensions
{
    [OrchardFeature("Piedone.Combinator")]
    internal static class ResourceDefinitionExtensions
    {
        /// <summary>
        /// Gets the ultimate full path of a resource, even if it uses CDN. Note that the paths are not uniform, they're
        /// concatenated from the resoure's paths, therefore they can well be virtual relative paths (starting with a tilde)
        /// or relative public urls.
        /// </summary>
        /// <remarks>
        /// This should really be an extension property. Waiting for C# 5.0...
        /// </remarks>
        public static string GetFullPath(this ResourceDefinition resource)
        {
            if (String.IsNullOrEmpty(resource.Url)) return resource.UrlCdn;

            if (resource.Url.Contains("~")) return resource.Url;

            return Path.Combine(resource.BasePath + resource.Url);
        }

        public static bool IsCDNResource(this ResourceDefinition resource)
        {
            return Uri.IsWellFormedUriString(resource.GetFullPath(), UriKind.Absolute);
        }
    }
}