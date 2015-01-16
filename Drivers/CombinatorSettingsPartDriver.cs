using System;
using System.Dynamic;
using System.Text.RegularExpressions;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.UI.Notify;
using Piedone.Combinator.EventHandlers;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;
using Piedone.HelpfulLibraries.Utilities;

namespace Piedone.Combinator.Drivers
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartDriver : ContentPartDriver<CombinatorSettingsPart>
    {
        private readonly ICacheFileService _cacheFileService;
        private readonly ICombinatorEventHandler _combinatorEventHandler;
        private readonly INotifier _notifier;

        protected override string Prefix
        {
            get { return "Combinator"; }
        }

        public Localizer T { get; set; }


        public CombinatorSettingsPartDriver(
            ICacheFileService cacheFileService,
            ICombinatorEventHandler combinatorEventHandler,
            INotifier notifier)
        {
            _cacheFileService = cacheFileService;
            _combinatorEventHandler = combinatorEventHandler;
            _notifier = notifier;

            T = NullLocalizer.Instance;
        }


        // GET
        protected override DriverResult Editor(CombinatorSettingsPart part, dynamic shapeHelper)
        {
            return Editor(part, null, shapeHelper);
        }

        // POST
        protected override DriverResult Editor(CombinatorSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            return ContentShape("Parts_CombinatorSettings_SiteSettings",
                () =>
                {
                    if (updater != null)
                    {
                        dynamic formerSettings = new ExpandoObject();
                        formerSettings.CombinationExcludeRegex = part.CombinationExcludeRegex;
                        formerSettings.CombineCdnResources = part.CombineCdnResources;
                        formerSettings.ResourceBaseUrl = part.ResourceBaseUrl;
                        formerSettings.MinifyResources = part.MinifyResources;
                        formerSettings.MinificationExcludeRegex = part.MinificationExcludeRegex;
                        formerSettings.EmbedCssImages = part.EmbedCssImages;
                        formerSettings.EmbeddedImagesMaxSizeKB = part.EmbeddedImagesMaxSizeKB;
                        formerSettings.EmbedCssImagesStylesheetExcludeRegex = part.EmbedCssImagesStylesheetExcludeRegex;
                        formerSettings.GenerateImageSprites = part.GenerateImageSprites;
                        formerSettings.EnableResourceSharing = part.EnableResourceSharing;
                        formerSettings.ResourceSharingExcludeRegex = part.ResourceSharingExcludeRegex;
                        formerSettings.ResourceSetRegexes = part.ResourceSetRegexes;

                        updater.TryUpdateModel(part, Prefix, null, null);


                        if (part.CombinationExcludeRegex != formerSettings.CombinationExcludeRegex
                            || part.CombineCdnResources != formerSettings.CombineCdnResources
                            || part.ResourceBaseUrl != formerSettings.ResourceBaseUrl
                            || part.MinifyResources != formerSettings.MinifyResources
                            || (part.MinifyResources && part.MinificationExcludeRegex != formerSettings.MinificationExcludeRegex)
                            || part.EmbedCssImages != formerSettings.EmbedCssImages
                            || (part.EmbedCssImages && part.EmbeddedImagesMaxSizeKB != formerSettings.EmbeddedImagesMaxSizeKB)
                            || (part.EmbedCssImages && part.EmbedCssImagesStylesheetExcludeRegex != formerSettings.EmbedCssImagesStylesheetExcludeRegex)
                            || part.GenerateImageSprites != formerSettings.GenerateImageSprites
                            || part.EnableResourceSharing != formerSettings.EnableResourceSharing
                            || part.ResourceSharingExcludeRegex != formerSettings.ResourceSharingExcludeRegex
                            || (part.ResourceSetRegexes != formerSettings.ResourceSetRegexes))
                        {
                            var valuesAreValid = true;

                            if (!string.IsNullOrEmpty(part.ResourceBaseUrl))
                            {
                                try
                                {
                                    UriHelper.CreateUri(part.ResourceBaseUrl);
                                }
                                catch (UriFormatException ex)
                                {
                                    valuesAreValid = false;
                                    updater.AddModelError("Combinator.ResourceBaseUrlMalformed", T("The resource base URL you provided is invalid: {0}", ex.Message));
                                }
                            }

                            if (!string.IsNullOrEmpty(part.CombinationExcludeRegex))
                            {
                                if (!TestRegex(part.CombinationExcludeRegex, T("combination exclude regex"), updater)) valuesAreValid = false;
                            }
                            if (!string.IsNullOrEmpty(part.MinificationExcludeRegex))
                            {
                                if (!TestRegex(part.MinificationExcludeRegex, T("minification exclude regex"), updater)) valuesAreValid = false;
                            }
                            if (!string.IsNullOrEmpty(part.EmbedCssImagesStylesheetExcludeRegex))
                            {
                                if (!TestRegex(part.EmbedCssImagesStylesheetExcludeRegex, T("embedded css images exclude regex"), updater)) valuesAreValid = false;
                            }
                            if (!string.IsNullOrEmpty(part.ResourceSharingExcludeRegex))
                            {
                                if (!TestRegex(part.ResourceSharingExcludeRegex, T("resource sharing exclude regex"), updater)) valuesAreValid = false;
                            }
                            if (!string.IsNullOrEmpty(part.ResourceSetRegexes))
                            {
                                int i = 1;
                                foreach (var regex in part.ResourceSetRegexesEnumerable)
                                {
                                    if (!TestRegex(regex, T("resource set regexes #{0}", i.ToString()), updater)) valuesAreValid = false;
                                    i++;
                                }
                            }

                            if (valuesAreValid)
                            {
                                // Not emptying the cache would cause inconsistencies
                                _cacheFileService.Empty();
                                _notifier.Information(T("Combinator cache emptied."));
                            }
                        }

                        _combinatorEventHandler.ConfigurationChanged(); 
                    }

                    return shapeHelper.EditorTemplate(
                        TemplateName: "Parts.CombinatorSettings.SiteSettings",
                        Model: part,
                        Prefix: Prefix);
                })
                .OnGroup("Combinator");
        }

        protected override void Exporting(CombinatorSettingsPart part, ExportContentContext context)
        {
            var element = context.Element(part.PartDefinition.Name);

            element.SetAttributeValue("CombinationExcludeRegex", part.CombinationExcludeRegex);
            element.SetAttributeValue("CombineCdnResources", part.CombineCdnResources);
            element.SetAttributeValue("ResourceBaseUrl", part.ResourceBaseUrl);
            element.SetAttributeValue("EnableForAdmin", part.EnableForAdmin);
            element.SetAttributeValue("MinifyResources", part.MinifyResources);
            element.SetAttributeValue("MinificationExcludeRegex", part.MinificationExcludeRegex);
            element.SetAttributeValue("EmbedCssImages", part.EmbedCssImages);
            element.SetAttributeValue("EmbeddedImagesMaxSizeKB", part.EmbeddedImagesMaxSizeKB);
            element.SetAttributeValue("EmbedCssImagesStylesheetExcludeRegex", part.EmbedCssImagesStylesheetExcludeRegex);
            element.SetAttributeValue("GenerateImageSprites", part.GenerateImageSprites);
            element.SetAttributeValue("EnableResourceSharing", part.EnableResourceSharing);
            element.SetAttributeValue("ResourceSharingExcludeRegex", part.ResourceSharingExcludeRegex);
            element.SetAttributeValue("ResourceSetRegexes", part.ResourceSetRegexes);
        }

        protected override void Importing(CombinatorSettingsPart part, ImportContentContext context)
        {
            var partName = part.PartDefinition.Name;

            context.ImportAttribute(partName, "CombinationExcludeRegex", value => part.CombinationExcludeRegex = value);
            context.ImportAttribute(partName, "CombineCdnResources", value => part.CombineCdnResources = bool.Parse(value));
            context.ImportAttribute(partName, "ResourceBaseUrl", value => part.ResourceBaseUrl = value);
            context.ImportAttribute(partName, "EnableForAdmin", value => part.EnableForAdmin = bool.Parse(value));
            context.ImportAttribute(partName, "MinifyResources", value => part.MinifyResources = bool.Parse(value));
            context.ImportAttribute(partName, "MinificationExcludeRegex", value => part.MinificationExcludeRegex = value);
            context.ImportAttribute(partName, "EmbedCssImages", value => part.EmbedCssImages = bool.Parse(value));
            context.ImportAttribute(partName, "EmbeddedImagesMaxSizeKB", value => part.EmbeddedImagesMaxSizeKB = int.Parse(value));
            context.ImportAttribute(partName, "EmbedCssImagesStylesheetExcludeRegex", value => part.EmbedCssImagesStylesheetExcludeRegex = value);
            context.ImportAttribute(partName, "GenerateImageSprites", value => part.GenerateImageSprites = bool.Parse(value));
            context.ImportAttribute(partName, "EnableResourceSharing", value => part.EnableResourceSharing = bool.Parse(value));
            context.ImportAttribute(partName, "ResourceSharingExcludeRegex", value => part.ResourceSharingExcludeRegex = value);
            context.ImportAttribute(partName, "ResourceSetRegexes", value => part.ResourceSetRegexes = value);
        }


        private bool TestRegex(string pattern, LocalizedString fieldName, IUpdateModel updater)
        {
            try
            {
                Regex.IsMatch("test", pattern);
            }
            catch (ArgumentException ex)
            {
                updater.AddModelError("Combinator." + fieldName.TextHint + "Malformed", T("There was a problem with the regex for {0} you provided: {1}", fieldName.Text, ex.Message));
                return false;
            }

            return true;
        }
    }
}