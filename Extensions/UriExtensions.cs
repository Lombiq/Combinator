using System;

namespace Piedone.Combinator.Extensions
{
    public static class UriExtensions
    {
        public static string ToStringWithoutScheme(this Uri uri)
        {
            if (!uri.IsAbsoluteUri) return uri.ToString();
            return "//" + uri.Host + uri.PathAndQuery;
        }
    }
}