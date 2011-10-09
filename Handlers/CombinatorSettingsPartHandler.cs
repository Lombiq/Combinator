using Orchard.ContentManagement.Handlers;
using Orchard.Environment.Extensions;
using Piedone.Combinator.Models;
using Orchard.Data;

namespace Piedone.Combinator
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartHandler : ContentHandler
    {
        public CombinatorSettingsPartHandler(IRepository<CombinatorSettingsPartRecord> repository)
        {
            Filters.Add(new ActivatingFilter<CombinatorSettingsPart>("Site"));
            Filters.Add(StorageFilter.For(repository));
        }
    }
}