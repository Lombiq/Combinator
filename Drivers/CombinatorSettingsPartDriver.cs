using System;
using System.Dynamic;
using System.Text.RegularExpressions;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Piedone.Combinator.EventHandlers;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;

namespace Piedone.Combinator.Drivers
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartDriver : ContentPartDriver<CombinatorSettingsPart>
    {
        private readonly ICacheFileService _cacheFileService;
        private readonly ICombinatorEventHandler _combinatorEventHandler;

        protected override string Prefix
        {
            get { return "Combinator"; }
        }

        public Localizer T { get; set; }


        public CombinatorSettingsPartDriver(
            ICacheFileService cacheFileService,
            ICombinatorEventHandler combinatorEventHandler)
        {
            _cacheFileService = cacheFileService;
            _combinatorEventHandler = combinatorEventHandler;

            T = NullLocalizer.Instance;
        }


        // GET
        protected override DriverResult Editor(CombinatorSettingsPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_CombinatorSettings_SiteSettings",
                    () => shapeHelper.EditorTemplate(
                            TemplateName: "Parts.CombinatorSettings.SiteSettings",
                            Model: part,
                            Prefix: Prefix))
                   .OnGroup("Combinator");
        }

        // POST
        protected override DriverResult Editor(CombinatorSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            dynamic formerSettings = new ExpandoObject();
            formerSettings.CombinationExcludeRegex = part.CombinationExcludeRegex;
            formerSettings.CombineCdnResources = part.CombineCdnResources;
            formerSettings.ResourceDomain = part.ResourceDomain;
            formerSettings.MinifyResources = part.MinifyResources;
            formerSettings.MinificationExcludeRegex = part.MinificationExcludeRegex;
            formerSettings.EmbedCssImages = part.EmbedCssImages;
            formerSettings.EmbeddedImagesMaxSizeKB = part.EmbeddedImagesMaxSizeKB;
            formerSettings.EmbedCssImagesStylesheetExcludeRegex = part.EmbedCssImagesStylesheetExcludeRegex;
            formerSettings.GenerateImageSprites = part.GenerateImageSprites;
            formerSettings.ResourceSetRegexes = part.ResourceSetRegexes;

            updater.TryUpdateModel(part, Prefix, null, null);


            if (part.CombinationExcludeRegex != formerSettings.CombinationExcludeRegex
                || part.CombineCdnResources != formerSettings.CombineCdnResources
                || part.ResourceDomain != formerSettings.ResourceDomain
                || part.MinifyResources != formerSettings.MinifyResources
                || (part.MinifyResources && part.MinificationExcludeRegex != formerSettings.MinificationExcludeRegex)
                || part.EmbedCssImages != formerSettings.EmbedCssImages
                || (part.EmbedCssImages && part.EmbeddedImagesMaxSizeKB != formerSettings.EmbeddedImagesMaxSizeKB)
                || (part.EmbedCssImages && part.EmbedCssImagesStylesheetExcludeRegex != formerSettings.EmbedCssImagesStylesheetExcludeRegex)
                || part.GenerateImageSprites != formerSettings.GenerateImageSprites
                || (part.ResourceSetRegexes != formerSettings.ResourceSetRegexes))
            {
                // Not emptying the cache would cause inconsistencies
                _cacheFileService.Empty();

                if (!string.IsNullOrEmpty(part.CombinationExcludeRegex)) TestRegex(part.CombinationExcludeRegex, T("combination exclude regex"), updater);
                if (!string.IsNullOrEmpty(part.MinificationExcludeRegex)) TestRegex(part.MinificationExcludeRegex, T("minification exclude regex"), updater);
                if (!string.IsNullOrEmpty(part.EmbedCssImagesStylesheetExcludeRegex)) TestRegex(part.EmbedCssImagesStylesheetExcludeRegex, T("embedded css images exclude regex"), updater);
                if (!string.IsNullOrEmpty(part.ResourceSetRegexes))
                {
                    int i = 1;
                    foreach (var regex in part.ResourceSetRegexesEnumerable)
                    {
                        TestRegex(regex, T("resource set regexes #{0}", i.ToString()), updater);
                        i++;
                    }
                }
            }

            _combinatorEventHandler.ConfigurationChanged();

            return Editor(part, shapeHelper);
        }

        protected override void Exporting(CombinatorSettingsPart part, ExportContentContext context)
        {
            var element = context.Element(part.PartDefinition.Name);

            element.SetAttributeValue("CombinationExcludeRegex", part.CombinationExcludeRegex);
            element.SetAttributeValue("CombineCdnResources", part.CombineCdnResources);
            element.SetAttributeValue("ResourceDomain", part.ResourceDomain);
            element.SetAttributeValue("MinifyResources", part.MinifyResources);
            element.SetAttributeValue("MinificationExcludeRegex", part.MinificationExcludeRegex);
            element.SetAttributeValue("EmbedCssImages", part.EmbedCssImages);
            element.SetAttributeValue("EmbeddedImagesMaxSizeKB", part.EmbeddedImagesMaxSizeKB);
            element.SetAttributeValue("EmbedCssImagesStylesheetExcludeRegex", part.EmbedCssImagesStylesheetExcludeRegex);
            element.SetAttributeValue("GenerateImageSprites", part.GenerateImageSprites);
            element.SetAttributeValue("ResourceSetRegexes", part.ResourceSetRegexes);
            element.SetAttributeValue("EnableForAdmin", part.EnableForAdmin);
        }

        protected override void Importing(CombinatorSettingsPart part, ImportContentContext context)
        {
            var partName = part.PartDefinition.Name;

            context.ImportAttribute(partName, "CombinationExcludeRegex", value => part.CombinationExcludeRegex = value);
            context.ImportAttribute(partName, "CombineCdnResources", value => part.CombineCdnResources = bool.Parse(value));
            context.ImportAttribute(partName, "ResourceDomain", value => part.ResourceDomain = value);
            context.ImportAttribute(partName, "MinifyResources", value => part.MinifyResources = bool.Parse(value));
            context.ImportAttribute(partName, "MinificationExcludeRegex", value => part.MinificationExcludeRegex = value);
            context.ImportAttribute(partName, "EmbedCssImages", value => part.EmbedCssImages = bool.Parse(value));
            context.ImportAttribute(partName, "EmbeddedImagesMaxSizeKB", value => part.EmbeddedImagesMaxSizeKB = int.Parse(value));
            context.ImportAttribute(partName, "EmbedCssImagesStylesheetExcludeRegex", value => part.EmbedCssImagesStylesheetExcludeRegex = value);
            context.ImportAttribute(partName, "GenerateImageSprites", value => part.GenerateImageSprites = bool.Parse(value));
            context.ImportAttribute(partName, "ResourceSetRegexes", value => part.ResourceSetRegexes = value);
            context.ImportAttribute(partName, "EnableForAdmin", value => part.EnableForAdmin = bool.Parse(value));
        }


        private void TestRegex(string pattern, LocalizedString fieldName, IUpdateModel updater)
        {
            try
            {
                Regex.IsMatch("test", pattern);
            }
            catch (ArgumentException ex)
            {
                updater.AddModelError("Combinator." + fieldName.TextHint + "Malformed", T("There was a problem with the regex for {0} you provided: {1}", fieldName.Text, ex.Message));
            }
        }
    }
}