
namespace Piedone.Combinator.Models
{
    public class CombinatorSettings : ICombinatorSettings
    {
        public string CombinationExcludeRegex { get; set; }
        public bool CombineCDNResources { get; set; }
        public bool MinifyResources { get; set; }
        public string MinificationExcludeRegex { get; set; }
        public bool EmbedCssImages { get; set; }
        public int EmbeddedImagesMaxSizeKB { get; set; }
        public string EmbedCssImagesStylesheetExcludeRegex { get; set; }
    }
}