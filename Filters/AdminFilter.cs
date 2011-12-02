using System.Web.Mvc;
using Orchard.Environment.Extensions;
using Orchard.Mvc.Filters;
using Orchard.UI.Resources;

namespace Piedone.Combinator.Filters
{
    /// <summary>
    /// Disables bundling in the Dashboard
    /// </summary>
    [OrchardFeature("Piedone.Combinator")]
    public class AdminFilter : FilterProvider, IAuthorizationFilter
    {
        private readonly IResourceManager _resourceManager;

        public AdminFilter(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
        }

        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var combinedResourceManager = _resourceManager as CombinedResourceManager;
            if (combinedResourceManager != null)
            {
                combinedResourceManager.IsDisabled = Orchard.UI.Admin.AdminFilter.IsApplied(filterContext.RequestContext);
            }
        }
    }
}