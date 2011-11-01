using Orchard;
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
        private readonly IOrchardServices _orchardServices;
        private readonly ICacheFileService _cacheFileService;

        public CombinatorSettingsPartDriver(
            IOrchardServices orchardServices,
            ICacheFileService cacheFileService)
        {
            _orchardServices = orchardServices;
            _cacheFileService = cacheFileService;
        }

        // GET
        protected override DriverResult Editor(CombinatorSettingsPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_CombinatorSettings_SiteSettings",
                    () =>
                    {
                        part.CacheFileCount = _cacheFileService.GetCount();

                        return shapeHelper.EditorTemplate(
                            TemplateName: "Parts.CombinatorSettings.SiteSettings",
                            Model: part,
                            Prefix: Prefix);
                    });
        }

        // POST
        protected override DriverResult Editor(CombinatorSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            var combineCDNResources = _orchardServices.WorkContext.CurrentSite.As<CombinatorSettingsPart>().CombineCDNResources;

            updater.TryUpdateModel(part, Prefix, null, null);

            // Not truncating the cache would cause inconsistencies
            if (part.CombineCDNResources != combineCDNResources)
            {
                _cacheFileService.Empty();
            }

            return Editor(part, shapeHelper);
        }
    }
}