using System;
using Orchard.UI.Resources;

namespace Piedone.Combinator.Extensions
{
    public static class RequireSettingsExtensions
    {
        public static bool IsConditional(this RequireSettings settings)
        {
            return !String.IsNullOrEmpty(settings.Condition);
        }
    }
}