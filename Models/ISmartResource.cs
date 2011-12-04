using Orchard;
using Orchard.UI.Resources;
using Piedone.Combinator.Helpers;

namespace Piedone.Combinator.Models
{
    public interface ISmartResource : ITransientDependency
    {
        string FullPath { get; }
        string PublicRelativeUrl { get; }
        string RelativeVirtualPath { get; set; }
        ResourceRequiredContext RequiredContext { get; set; }
        ResourceDefinition Resource { get; set; }
        RequireSettings Settings { get; set; }
        bool IsCDNResource { get; }
        bool IsConditional { get; }
        string UrlOverride { get; set; }
        ResourceType Type { get; set; }
        string Content { get; set; }
        SmartResource FillRequiredContext(string publicUrl, ResourceType resourceType);
        bool SerializableSettingsEqual(ISmartResource other);
        string GetSerializedSettings();
        void FillSettingsFromSerialization(string settings);
    }
}
