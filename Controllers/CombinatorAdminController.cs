using System;
using System.Web.Mvc;
using Piedone.Combinator.Services;
using Orchard.Mvc.Extensions;
using Orchard;
using Orchard.Environment.Extensions;
using Orchard.Security;

namespace Piedone.Combinator.Controllers
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorAdminController : Controller
    {
        private readonly IOrchardServices _orchardServices;
        private readonly ICacheFileService _cacheFileService;

        public CombinatorAdminController(
            IOrchardServices orchardServices,
            ICacheFileService cacheFileService)
        {
            _orchardServices = orchardServices;
            _cacheFileService = cacheFileService;
        }

        //[HttpPost]
        public ActionResult EmptyCache(string returnUrl = "")
        {
            if (_orchardServices.Authorizer.Authorize(StandardPermissions.SiteOwner))
            {
                _cacheFileService.Empty();
            }

            return this.RedirectLocal(returnUrl); // this necessary, as this is from an extension (Orchard.Mvc.Extensions.ControllerExtensions)
        }
    }
}
