using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Orchard;
using Orchard.Environment;
using Orchard.Environment.Extensions;
using Orchard.UI.Resources;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
using System.IO;
using System.Web;
using Piedone.HelpfulLibraries.Serialization;
using Orchard.Mvc;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class SmartResource : ISmartResource
    {
        private readonly HttpContextBase _httpContext;
        private readonly Work<ISimpleSerializer> _serializerWork;

        #region Path handling
        private string ApplicationPath
        {
            get
            {
                return _httpContext.Request.ApplicationPath;
            }
        }

        private string FullPath
        {
            get
            {
                return RequiredContext.Resource.GetFullPath();
            }
        }

        public Uri PublicUrl
        {
            get
            {
                if (IsCDNResource) return new Uri(FullPath);
                else return new Uri(_httpContext.Request.Url, PublicRelativeUrl);
            }
        }

        private string PublicRelativeUrl
        {
            get
            {
                return VirtualPathUtility.ToAbsolute(RelativeVirtualPath, ApplicationPath);
            }
        }

        public string RelativeVirtualPath
        {
            get
            {
                return VirtualPathUtility.ToAppRelative(FullPath, ApplicationPath);
            }
        }
        #endregion

        #region Public properties
        public ResourceRequiredContext RequiredContext { get; set; }

        public ResourceDefinition Resource
        {
            get { return RequiredContext.Resource; }
            set { RequiredContext.Resource = value; }
        }

        public RequireSettings Settings
        {
            get { return RequiredContext.Settings; }
            set { RequiredContext.Settings = value; }
        }

        public bool IsCDNResource
        {
            get
            {
                var fullPath = FullPath;

                return Uri.IsWellFormedUriString(fullPath, UriKind.Absolute)
                    && new Uri(fullPath).Host != _httpContext.Request.Url.Host;
            }
        }

        public bool IsConditional
        {
            get { return !String.IsNullOrEmpty(RequiredContext.Settings.Condition); }
        }

        public bool CombinedUrlIsOverridden { get; private set; }
        public ResourceType Type { get; set; }
        public string Content { get; set; }
        #endregion

        public SmartResource(
            IHttpContextAccessor httpContextAccessor,
            Work<ISimpleSerializer> serializerWork)
        {
            _httpContext = httpContextAccessor.Current();
            _serializerWork = serializerWork;

            CombinedUrlIsOverridden = false;
        }
        
        public void OverrideCombinedUrl(Uri url)
        {
            if (url != null && !String.IsNullOrEmpty(url.ToString()) && RequiredContext != null) Resource.SetUrl(url.ToString(), null);
            CombinedUrlIsOverridden = true;
        }

        public void FillRequiredContext(string url, ResourceType resourceType, string serializedSettings = "")
        {
            Type = resourceType;

            var stringResourceType = ResourceTypeHelper.EnumToStringType(resourceType);

            var resourceManifest = new ResourceManifest();
            resourceManifest.DefineResource(stringResourceType, url); // SetUrl() doesn't work here for some reason

            RequiredContext = new ResourceRequiredContext();
            Resource = new ResourceDefinition(resourceManifest, stringResourceType, url);
            Resource.SetUrl(url);
            Settings = new RequireSettings();

            if (!String.IsNullOrEmpty(serializedSettings)) FillSettingsFromSerialization(serializedSettings);
        }

        public void FillRequiredContext(string name, string url, ResourceType resourceType, string serializedSettings = "")
        {
            // Slash in front of name to avoid exception from ResourceManager.FixPath()
            FillRequiredContext(name, resourceType, serializedSettings);
            Resource.SetUrl(url);
        }

        #region Serialization
        [DataContract]
        public class SerializableSettings
        {
            [DataMember]
            public Uri UrlOverride { get; set; }

            [DataMember]
            public string Culture { get; set; }

            [DataMember]
            public string Condition { get; set; }

            [DataMember]
            public Dictionary<string, string> Attributes { get; set; }
        }

        public bool SettingsEqual(ISmartResource other)
        {
            // If one's RequiredContext is null, their settings are not identical...
            if (RequiredContext == null ^ other.RequiredContext == null) return false;

            // However if both of them are null, we say the settings are identical
            if (RequiredContext == null && other.RequiredContext == null) return true;

            return
                CombinedUrlIsOverridden == other.CombinedUrlIsOverridden
                && Settings.Culture == other.Settings.Culture
                && Settings.Condition == other.Settings.Condition
                && Settings.AttributesEqual(other.Settings);
        }

        public string GetSerializedSettings()
        {
            return _serializerWork.Value.Serialize(
                new SerializableSettings()
                    {
                        UrlOverride = CombinedUrlIsOverridden ? new Uri(Resource.Url) : null,
                        Culture = Settings.Culture,
                        Condition = Settings.Condition,
                        Attributes = Settings.Attributes
                    });
        }

        public void FillSettingsFromSerialization(string serialization)
        {
            if (String.IsNullOrEmpty(serialization)) return;

            if (Settings == null) Settings = new RequireSettings();

            var settings = _serializerWork.Value.Deserialize<SerializableSettings>(serialization);

            if (settings.UrlOverride != null) OverrideCombinedUrl(settings.UrlOverride);
            Settings.Culture = settings.Culture;
            Settings.Condition = settings.Condition;
            Settings.Attributes = settings.Attributes;
        }
        #endregion
    }
}