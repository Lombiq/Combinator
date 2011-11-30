using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Piedone.Combinator.Models;
using Orchard.Localization;
using Orchard.ContentManagement;

namespace Piedone.Combinator
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartHandler : ContentHandler
    {
        public Localizer T { get; set; }

        public CombinatorSettingsPartHandler(IRepository<CombinatorSettingsPartRecord> repository)
        {
            Filters.Add(new ActivatingFilter<CombinatorSettingsPart>("Site"));
            Filters.Add(StorageFilter.For(repository));

            T = NullLocalizer.Instance;
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