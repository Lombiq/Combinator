using System;
using System.Text.RegularExpressions;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettings : ICombinatorSettings
    {
        public Regex CombinationExcludeFilter { get; set; }
        public bool CombineCdnResources { get; set; }
        public Regex RemoteStorageUrlPattern { get; set; }
        public Uri ResourceBaseUri { get; set; }
        public bool MinifyResources { get; set; }
        public Regex MinificationExcludeFilter { get; set; }
        public bool EmbedCssImages { get; set; }
        public int EmbeddedImagesMaxSizeKB { get; set; }
        public Regex EmbedCssImagesStylesheetExcludeFilter { get; set; }
        public bool GenerateImageSprites { get; set; }
        public bool EnableResourceSharing { get; set; }
        public Regex ResourceSharingExcludeFilter { get; set; }
        public Regex[] ResourceSetFilters { get; set; }


        public CombinatorSettings()
        {
        }

        public CombinatorSettings(ICombinatorSettings previous)
        {
            CombinationExcludeFilter = previous.CombinationExcludeFilter;
            CombineCdnResources = previous.CombineCdnResources;
            RemoteStorageUrlPattern = previous.RemoteStorageUrlPattern;
            ResourceBaseUri = previous.ResourceBaseUri;
            MinifyResources = previous.MinifyResources;
            MinificationExcludeFilter = previous.MinificationExcludeFilter;
            EmbedCssImages = previous.EmbedCssImages;
            EmbeddedImagesMaxSizeKB = previous.EmbeddedImagesMaxSizeKB;
            EmbedCssImagesStylesheetExcludeFilter = previous.EmbedCssImagesStylesheetExcludeFilter;
            GenerateImageSprites = previous.GenerateImageSprites;
            EnableResourceSharing = previous.EnableResourceSharing;
            ResourceSharingExcludeFilter = previous.ResourceSharingExcludeFilter;
            ResourceSetFilters = previous.ResourceSetFilters;
        }
    }
}