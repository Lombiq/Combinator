using System;
using System.Text;
using Orchard.Environment.Extensions;
using Orchard.UI.Resources;

namespace Piedone.Combinator.Extensions
{
    public static class RequireSettingsExtensions
    {
        public static bool IsConditional(this RequireSettings settings)
        {
            return !String.IsNullOrEmpty(settings.Condition);
        }

        public static bool AttributesEqual(this RequireSettings settings, RequireSettings other)
        {
            return settings.StringifyAttributes() == other.StringifyAttributes();
        }

        public static string StringifyAttributes(this RequireSettings settings)
        {
            if (!settings.HasAttributes) return "";

            var sb = new StringBuilder();
            foreach (var item in settings.Attributes)
            {
                sb.Append(item);
            }

            return sb.ToString();
        }
    }
}