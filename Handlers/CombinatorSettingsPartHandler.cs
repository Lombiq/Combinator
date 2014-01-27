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
        }


        protected override void GetItemMetadata(GetContentItemMetadataContext context)
        {
            if (context.ContentItem.ContentType != "Site")
                return;

            base.GetItemMetadata(context);

            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("Combinator")) { Id = "Combinator" });
        }
    }
}