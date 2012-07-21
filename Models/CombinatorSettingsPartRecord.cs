using Orchard.ContentManagement.Records;
using Orchard.Environment.Extensions;
using Orchard.Data.Conventions;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartRecord : ContentPartRecord
    {
        [StringLengthMax]
        public virtual string CombinationExcludeRegex { get; set; }

        [StringLengthMax]
        public virtual bool CombineCdnResources { get; set; }

        [StringLengthMax]
        public virtual bool MinifyResources { get; set; }

        [StringLengthMax]
        public virtual string MinificationExcludeRegex { get; set; }

        [StringLengthMax]
        public virtual bool EmbedCssImages { get; set; }

        [StringLengthMax]
        public virtual int EmbeddedImagesMaxSizeKB { get; set; }

        [StringLengthMax]
        public virtual string EmbedCssImagesStylesheetExcludeRegex { get; set; }

        [StringLengthMax]
        public virtual string ResourceSetRegexes { get; set; }

        [StringLengthMax]
        public virtual bool EnableForAdmin { get; set; }

        public CombinatorSettingsPartRecord()
        {
            CombinationExcludeRegex = "";
            CombineCdnResources = false;
            MinifyResources = true;
            MinificationExcludeRegex = "";
            EmbedCssImages = false;
            EmbeddedImagesMaxSizeKB = 15;
            EmbedCssImagesStylesheetExcludeRegex = "";
            ResourceSetRegexes = "";
            EnableForAdmin = false;
        }
    }
}