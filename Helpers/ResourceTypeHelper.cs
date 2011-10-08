using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Helpers
{
    public enum ResourceType
    {
        Style,
        JavaScript
    }

    [OrchardFeature("Piedone.Combinator")]
    public class ResourceTypeHelper
    {
        public static ResourceType StringTypeToEnum(string resourceType)
        {
            if (resourceType == "stylesheet")
            {
                return ResourceType.Style;
            }

            return ResourceType.JavaScript;
        }

        public static string EnumToStringType(ResourceType resourceType)
        {
            if (resourceType == ResourceType.Style)
            {
                return "stylesheet";
            }

            return "script";
        }
    }
}