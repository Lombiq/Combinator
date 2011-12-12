using Orchard.ContentManagement.Records;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartRecord : ContentPartRecord, ICombinatorSettings
    {
        public virtual string CombinationExcludeRegex { get; set; }
        public virtual bool CombineCDNResources { get; set; }
        public virtual bool MinifyResources { get; set; }
        public virtual string MinificationExcludeRegex { get; set; }
        public virtual bool EmbedCssImages { get; set; }
        public virtual int EmbeddedImagesMaxSizeKB { get; set; }

        public CombinatorSettingsPartRecord()
        {
            CombinationExcludeRegex = "";
            CombineCDNResources = false;
            MinifyResources = true;
            MinificationExcludeRegex = "";
            EmbedCssImages = false;
            EmbeddedImagesMaxSizeKB = 15;
        }
    }
}