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

        public int CacheFileCount { get; set; }
    }
}