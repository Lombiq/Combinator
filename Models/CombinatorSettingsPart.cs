using Orchard.ContentManagement;
using Orchard.Core.Common.Utilities;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPart : ContentPart<CombinatorSettingsPartRecord>, ICombinatorSettings
    {
        public string CombinationExcludeRegex
        {
            get { return Record.CombinationExcludeRegex; }
            set { Record.CombinationExcludeRegex = value; }
        }

        public bool CombineCDNResources
        {
            get { return Record.CombineCDNResources; }
            set { Record.CombineCDNResources = value; }
        }

        public bool MinifyResources
        {
            get { return Record.MinifyResources; }
            set { Record.MinifyResources = value; }
        }

        public string MinificationExcludeRegex
        {
            get { return Record.MinificationExcludeRegex; }
            set { Record.MinificationExcludeRegex = value; }
        }

        public bool EmbedCssImages
        {
            get { return Record.EmbedCssImages; }
            set { Record.EmbedCssImages = value; }
        }

        public int EmbeddedImagesMaxSizeKB
        {
            get { return Record.EmbeddedImagesMaxSizeKB; }
            set { Record.EmbeddedImagesMaxSizeKB = value; }
        }

        private readonly LazyField<int> _cacheFileCount = new LazyField<int>();
        public LazyField<int> CacheFileCountField { get { return _cacheFileCount; } }
        public int CacheFileCount
        {
            get { return _cacheFileCount.Value; }
        }
    }
}