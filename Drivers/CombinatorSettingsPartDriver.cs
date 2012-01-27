using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Environment.Extensions;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;

namespace Piedone.Combinator.Drivers
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartDriver : ContentPartDriver<CombinatorSettingsPart>
    {
        private readonly ICacheFileService _cacheFileService;

        protected override string Prefix
        {
            get { return "Combinator"; }
        }

        public CombinatorSettingsPartDriver(ICacheFileService cacheFileService)
        {
            _cacheFileService = cacheFileService;
        }

        // GET
        protected override DriverResult Editor(CombinatorSettingsPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_CombinatorSettings_SiteSettings",
                    () =>   shapeHelper.EditorTemplate(
                            TemplateName: "Parts.CombinatorSettings.SiteSettings",
                            Model: part,
                            Prefix: Prefix))
                   .OnGroup("Combinator");
        }

        // POST
        protected override DriverResult Editor(CombinatorSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            var formerSettings = new CombinatorSettingsPartRecord();
            formerSettings.CombinationExcludeRegex = part.CombinationExcludeRegex;
            formerSettings.CombineCDNResources = part.CombineCDNResources;
            formerSettings.MinifyResources = part.MinifyResources;
            formerSettings.MinificationExcludeRegex = part.MinificationExcludeRegex;
            formerSettings.EmbedCssImages = part.EmbedCssImages;
            formerSettings.EmbeddedImagesMaxSizeKB = part.EmbeddedImagesMaxSizeKB;
            formerSettings.EmbedCssImagesStylesheetExcludeRegex = part.EmbedCssImagesStylesheetExcludeRegex;
            formerSettings.ResourceSetRegexes = part.ResourceSetRegexes;

            updater.TryUpdateModel(part, Prefix, null, null);

            // Not emptying the cache would cause inconsistencies
            if (part.CombinationExcludeRegex != formerSettings.CombinationExcludeRegex
                || part.CombineCDNResources != formerSettings.CombineCDNResources
                || part.MinifyResources != formerSettings.MinifyResources
                || (part.MinifyResources && part.MinificationExcludeRegex != formerSettings.MinificationExcludeRegex)
                || part.EmbedCssImages != formerSettings.EmbedCssImages
                || (part.EmbedCssImages && part.EmbeddedImagesMaxSizeKB != formerSettings.EmbeddedImagesMaxSizeKB)
                || (part.EmbedCssImages && part.EmbedCssImagesStylesheetExcludeRegex != formerSettings.EmbedCssImagesStylesheetExcludeRegex)
                || (part.ResourceSetRegexes != formerSettings.ResourceSetRegexes))
            {
                _cacheFileService.Empty();
            }

            return Editor(part, shapeHelper);
        }
    }
}