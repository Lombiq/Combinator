using System;
using Orchard;
using Orchard.UI.Resources;
using Piedone.Combinator.Helpers;

namespace Piedone.Combinator.Models
{
    public interface ISmartResource : ITransientDependency
    {
        Uri PublicUrl { get; }
        string RelativeVirtualPath { get; }
        ResourceRequiredContext RequiredContext { get; set; }
        ResourceDefinition Resource { get; set; }
        RequireSettings Settings { get; set; }
        bool IsCDNResource { get; }
        bool IsConditional { get; }
        bool CombinedUrlIsOverridden { get; }
        ResourceType Type { get; set; }
        DateTime LastUpdatedUtc { get; set; }
        string Content { get; set; }
        void OverrideCombinedUrl(Uri url);
        void FillRequiredContext(string name, ResourceType resourceType, string url, string serializedSettings = "");
        void FillRequiredContext(ResourceRequiredContext requiredContext);
        bool SettingsEqual(ISmartResource other);
        string GetSerializedSettings();
        void FillSettingsFromSerialization(string settings);
    }

    public static class SmartResourceExtensions
    {
        public static void FillRequiredContext(this ISmartResource resource, string url, ResourceType resourceType, string serializedSettings = "")
        {
            resource.FillRequiredContext(url, resourceType, url, serializedSettings);
        }
    }
}
