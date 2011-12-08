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
        Uri UrlOverride { get; set; }
        ResourceType Type { get; set; }
        string Content { get; set; }
        void FillRequiredContext(string url, ResourceType resourceType, string serializedSettings = "");
        void FillRequiredContext(string name, string url, ResourceType resourceType, string serializedSettings = "");
        bool SerializableSettingsEqual(ISmartResource other);
        string GetSerializedSettings();
        void FillSettingsFromSerialization(string settings);
    }
}
