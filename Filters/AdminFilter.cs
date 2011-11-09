using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Orchard.Environment.Extensions;
using Orchard.Mvc.Filters;
using Orchard.UI.Admin;

namespace Piedone.Combinator.Filters
{
    /// <summary>
    /// Purpose of this class is to detect whether we are in the admin area or not
    /// </summary>
    /// <remarks>
    /// Taken from Orchard.UI.Admin.AdminFilter as advised by contributor Znowman.
    /// </remarks>
    [OrchardFeature("Piedone.Combinator")]
    public class AdminFilter : FilterProvider, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            CombinedResourceManager.IsDisabled = IsAdmin(filterContext);
        }

        private static bool IsAdmin(AuthorizationContext filterContext)
        {
            if (IsNameAdmin(filterContext) || IsNameAdminProxy(filterContext))
            {
                return true;
            }

            var adminAttributes = GetAdminAttributes(filterContext.ActionDescriptor);
            if (adminAttributes != null && adminAttributes.Any())
            {
                return true;
            }
            return false;
        }

        private static bool IsNameAdmin(AuthorizationContext filterContext)
        {
            return string.Equals(filterContext.ActionDescriptor.ControllerDescriptor.ControllerName, "Admin",
                                 StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNameAdminProxy(AuthorizationContext filterContext)
        {
            return filterContext.ActionDescriptor.ControllerDescriptor.ControllerName.StartsWith(
                "AdminControllerProxy", StringComparison.InvariantCultureIgnoreCase) &&
                filterContext.ActionDescriptor.ControllerDescriptor.ControllerName.Length == "AdminControllerProxy".Length + 32;
        }

        private static IEnumerable<AdminAttribute> GetAdminAttributes(ActionDescriptor descriptor)
        {
            return descriptor.GetCustomAttributes(typeof(AdminAttribute), true)
                .Concat(descriptor.ControllerDescriptor.GetCustomAttributes(typeof(AdminAttribute), true))
                .OfType<AdminAttribute>();
        }

    }
}