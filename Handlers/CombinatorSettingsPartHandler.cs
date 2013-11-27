using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Piedone.Combinator.Models;
using Piedone.Combinator.Services;

namespace Piedone.Combinator
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartHandler : ContentHandler
    {
        public Localizer T { get; set; }


        public CombinatorSettingsPartHandler( Work<ICacheFileService> cacheFileServiceWork)
        {
            Filters.Add(new ActivatingFilter<CombinatorSettingsPart>("Site"));

            T = NullLocalizer.Instance;

            OnActivated<CombinatorSettingsPart>((context, part) =>
                {
                    // Add loaders that will load content just-in-time
                    part.CacheFileCountField.Loader(() => cacheFileServiceWork.Value.GetCount());
                });

            OnInitializing<CombinatorSettingsPart>((context, part) =>
                {
                    part.MinifyResources = true;
                    part.MinificationExcludeRegex = ".min";
                    part.EmbeddedImagesMaxSizeKB = 15;
                });
        }


        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            if (context.ContentItem.ContentType != "Site")
                return;
            base.GetItemMetadata(context);
            var groupInfo = new GroupInfo(T("Combinator")); // Addig a new group to the "Settings" menu.
            groupInfo.Id = "Combinator";
            context.Metadata.EditorGroupInfo.Add(groupInfo);
        }
    }
}