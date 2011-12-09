using Orchard;
using Orchard.UI.Resources;
using Piedone.Combinator.Helpers;
using System;

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
        string Content { get; set; }
        void OverrideCombinedUrl(Uri url);
        void FillRequiredContext(string url, ResourceType resourceType, string serializedSettings = "");
        void FillRequiredContext(string name, string url, ResourceType resourceType, string serializedSettings = "");
        bool SettingsEqual(ISmartResource other);
        string GetSerializedSettings();
        void FillSettingsFromSerialization(string settings);
    }
}
