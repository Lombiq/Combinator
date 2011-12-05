using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using Orchard;
using Orchard.Environment;
using Orchard.Environment.Extensions;
using Orchard.UI.Resources;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class SmartResource : ISmartResource
    {
        #region Private fields and properties
        private readonly Work<IResourceManager> _resourceManagerWork;
        private readonly Work<WorkContext> _workContextWork;

        private IResourceManager _resourceManager;
        private IResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null) _resourceManager = _resourceManagerWork.Value;
                return _resourceManager;
            }
        }
        #endregion

        #region Path handling
        private string _applicationPath;
        private string ApplicationPath
        {
            get
            {
                if (_applicationPath == null) _applicationPath = _workContextWork.Value.HttpContext.Request.ApplicationPath;
                return _applicationPath;
            }
            set { _applicationPath = value; }
        }

        public string FullPath
        {
            get { return RequiredContext.Resource.GetFullPath(); }
        }

        public string PublicRelativeUrl
        {
            get
            {
                var url = RelativeVirtualPath.Remove(0, 1); // Removing the tilde
                return (ApplicationPath != "/") ? ApplicationPath + url : url;
            }
        }

        private string _fullPathReference;
        private string _relativeVirtualPath;
        public string RelativeVirtualPath
        {
            get
            {
                if (String.IsNullOrEmpty(_relativeVirtualPath))
                {
                    var path = FullPath;

                    if (String.IsNullOrEmpty(_fullPathReference) || _fullPathReference != path)
                    {
                        _fullPathReference = path;
                        if (path.StartsWith(ApplicationPath))
                        {
                            // Strips e.g. /OrchardLocal
                            if (ApplicationPath != "/")
                            {
                                int place = path.IndexOf(ApplicationPath);
                                // Finds the first occurence and replaces it with empty string
                                path = path.Remove(place, ApplicationPath.Length).Insert(place, "");
                            }

                            path = "~" + path;
                        }

                        _relativeVirtualPath = path;
                    }
                }

                return _relativeVirtualPath;
            }
            set { _relativeVirtualPath = value; }
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
            get { return RequiredContext.Resource.IsCDNResource(); }
        }

        public bool IsConditional
        {
            get { return !String.IsNullOrEmpty(RequiredContext.Settings.Condition); }
        }

        private string _urlOverride;
        public string UrlOverride
        {
            get { return _urlOverride; }
            set
            {
                _urlOverride = value;
                if (!String.IsNullOrEmpty(value)) Resource.SetUrl(value, null);
            }
        }

        public ResourceType Type { get; set; }

        public string Content { get; set; }
        #endregion

        public SmartResource(
            Work<IResourceManager> resourceManagerWork,
            Work<WorkContext> workContextWork)
        {
            _resourceManagerWork = resourceManagerWork;
            _workContextWork = workContextWork;
        }

        // Looks unneeded
        public ISmartResource FillRequiredContext(string publicUrl, ResourceType resourceType)
        {
            Type = resourceType;

            RequiredContext = new ResourceRequiredContext();

            // This is only necessary to build the ResourceRequiredContext object, therefore we also delete the resource
            // from the required ones.
            RequiredContext.Settings = ResourceManager.Include(ResourceTypeHelper.EnumToStringType(resourceType), publicUrl, publicUrl);
            RequiredContext.Resource = ResourceManager.FindResource(RequiredContext.Settings);
            ResourceManager.NotRequired(ResourceTypeHelper.EnumToStringType(resourceType), RequiredContext.Resource.Name);

            return this;
        }

        #region Serialization
        [DataContract]
        public class SerializableSettings
        {
            [DataMember]
            public string UrlOverride { get; set; }

            [DataMember]
            public string Culture { get; set; }

            [DataMember]
            public string Condition { get; set; }

            [DataMember]
            public Dictionary<string, string> Attributes { get; set; }
        }

        public bool SerializableSettingsEqual(ISmartResource other)
        {
            if (RequiredContext == null ^ other.RequiredContext == null
                && RequiredContext == null && other.RequiredContext == null) return false;

            return
                UrlOverride == other.UrlOverride
                && Settings.Culture == other.Settings.Culture
                && Settings.Condition == other.Settings.Condition
                && Settings.AttributesEqual(other.Settings);
        }

        public string GetSerializedSettings()
        {
            return Piedone.HelpfulLibraries.Serialization.Helpers.Serializer.Serialize(
                new SerializableSettings()
                    {
                        UrlOverride = UrlOverride,
                        Culture = Settings.Culture,
                        Condition = Settings.Condition,
                        Attributes = Settings.Attributes
                    });
        }

        public void FillSettingsFromSerialization(string serialization)
        {
            if (String.IsNullOrEmpty(serialization)) return;

            var settings = Piedone.HelpfulLibraries.Serialization.Helpers.Serializer.Deserialize<SerializableSettings>(serialization);

            UrlOverride = settings.UrlOverride;
            Settings.Culture = settings.Culture;
            Settings.Condition = settings.Condition;
            Settings.Attributes = settings.Attributes;
        }
        #endregion
    }
}