using System;
using System.Text.RegularExpressions;

namespace Piedone.Combinator.Models
{
    public interface ICombinatorSettings
    {
        Regex CombinationExcludeFilter { get; }
        bool CombineCdnResources { get; }
        Regex RemoteStorageUrlPattern { get; }
        Uri ResourceBaseUri { get; }
        bool MinifyResources { get; }
        Regex MinificationExcludeFilter { get; }
        bool EmbedCssImages { get; }
        int EmbeddedImagesMaxSizeKB { get; }
        Regex EmbedCssImagesStylesheetExcludeFilter { get; }
        bool GenerateImageSprites { get; }
        bool EnableResourceSharing { get; }
        Regex ResourceSharingExcludeFilter { get; }
        Regex[] ResourceSetFilters { get; }
    }
}
