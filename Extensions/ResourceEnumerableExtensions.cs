using System.Collections.Generic;
using System.Linq;
using Orchard.UI.Resources;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Extensions
{
    /// <summary>
    /// Extensions for <see cref="Orchard.UI.Resources.ResourceRequiredContext"/> enumerables.
    /// </summary>
    /// <remarks>
    /// Using such string "fingerprints" is needed to distinguish between different resource outputs, depending on settings. This is
    /// needed for resource sharing. However it's not perfect as e.g. it's nowhere reflected if, although minifying is enabled, a certain
    /// resource was excluded from minification...
    /// </remarks>
    public static class ResourceEnumerableExtensions
    {
        public static string GetResourceListFingerprint<T>(this IEnumerable<T> resources, ICombinatorSettings settings) where T : ResourceRequiredContext
        {
            var key = string.Empty;

            resources.ToList().ForEach(resource => key += resource.Resource.GetFullPath() + "__");

            return 
                key.GetHashCode() + "-" + 
                (settings.MinifyResources ? "min" : "nomin") + "-" +
                (settings.EmbedCssImages ? "embed" + settings.EmbeddedImagesMaxSizeKB : "noembed") + "-" +
                (settings.GenerateImageSprites ? "sprites" : "nosprites");
        }

        public static IList<T> SetLocation<T>(this IList<T> resources, ResourceLocation location) where T : ResourceRequiredContext
        {
            resources.ToList().ForEach(resource => resource.Settings.Location = location);
            return resources;
        }

        public static string GetCombinatorResourceListFingerprint<T>(this IEnumerable<T> resources, ICombinatorSettings settings) where T : CombinatorResource
        {
            return resources.Select(resource => resource.RequiredContext).GetResourceListFingerprint(settings);
        }
    }
}