using Orchard.ContentManagement;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPart : ContentPart<CombinatorSettingsPartRecord>
    {
        public bool CombineCDNResources
        {
            get { return Record.CombineCDNResources; }
            set { Record.CombineCDNResources = value; }
        }

        public int CacheFileCount { get; set; }
    }
}