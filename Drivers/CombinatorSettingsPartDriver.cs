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

        protected override string Prefix
        {
            get { return "Combinator"; }
        }

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
                            
                    }).OnGroup("Combinator");
        }

        // POST
        protected override DriverResult Editor(CombinatorSettingsPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            var combineCDNResourcesFormer = part.CombineCDNResources;
            var minifyResourcesFormer = part.MinifyResources;
            var minificationExcludeRegexFormer = part.MinificationExcludeRegex;

            updater.TryUpdateModel(part, Prefix, null, null);

            // Not emptying the cache would cause inconsistencies
            if (part.CombineCDNResources != combineCDNResourcesFormer 
                || part.MinifyResources != minifyResourcesFormer
                || (part.MinifyResources && part.MinificationExcludeRegex != minificationExcludeRegexFormer))
            {
                _cacheFileService.Empty();
            }

            return Editor(part, shapeHelper);
        }
    }
}