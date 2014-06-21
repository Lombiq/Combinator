using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment;
using Orchard.Environment.Configuration;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;

namespace Piedone.Combinator
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartHandler : ContentHandler
    {
        private readonly ShellSettings _shellSettings;

        public Localizer T { get; set; }


        public CombinatorSettingsPartHandler(ShellSettings shellSettings, Work<ICacheFileService> cacheFileServiceWork)
        {
            _shellSettings = shellSettings;

            Filters.Add(new ActivatingFilter<CombinatorSettingsPart>("Site"));

            T = NullLocalizer.Instance;

            OnActivated<CombinatorSettingsPart>((context, part) =>
                {
                    part.CacheFileCountField.Loader(() => cacheFileServiceWork.Value.GetCount());
                    // Paths containing the name of the shell (e.g. Media URLs) or a query string should be excluded.
                    part.ResourceSharingExcludeRegexDefaultField.Loader(() => "/" + shellSettings.Name + "/|\\?");
                });
        }


        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            if (context.ContentItem.ContentType != "Site")
                return;

            base.GetItemMetadata(context);

            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("Combinator")) { Id = "Combinator" });
        }

        protected override void Imported(ImportContentContext context)
        {
            EnforceResourceSharingOffOnDefault(context);
        }

        protected override void Updated(UpdateContentContext context)
        {
            EnforceResourceSharingOffOnDefault(context);
        }


        private void EnforceResourceSharingOffOnDefault(ContentContextBase context)
        {
            if (context.ContentItem.Id != 1) return;

            var settingsPart = context.ContentItem.As<CombinatorSettingsPart>();

            if (_shellSettings.Name == ShellSettings.DefaultName && settingsPart.EnableResourceSharing)
            {
                settingsPart.EnableResourceSharing = false;
            }
        }
    }
}