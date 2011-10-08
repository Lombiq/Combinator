using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Environment.Extensions;
using Orchard.UI.Resources;

namespace Piedone.Combinator.Extensions
{
    [OrchardFeature("Piedone.Combinator")]
    internal static class ResourceDefinitionExtension
    {
        /// <summary>
        /// Gets the ultimate full path of a resource, even if it uses CDN
        /// </summary>
        /// <remarks>
        /// This should really be an extension property. Waiting for C# 5.0...
        /// </remarks>
        public static string GetFullPath(this ResourceDefinition resource)
        {
            return !String.IsNullOrEmpty(resource.Url) ? resource.BasePath + resource.Url : resource.UrlCdn;
        }
    }
}