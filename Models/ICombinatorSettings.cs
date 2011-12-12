
namespace Piedone.Combinator.Models
{
    public interface ICombinatorSettings
    {
        string CombinationExcludeRegex { get; }
        bool CombineCDNResources { get; }
        bool MinifyResources { get; }
        string MinificationExcludeRegex { get; }
        bool EmbedCssImages { get; }
        int EmbeddedImagesMaxSizeKB { get; }
        string EmbedCssImagesStylesheetExcludeRegex { get; }
    }
}
