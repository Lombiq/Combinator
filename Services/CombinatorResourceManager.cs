using System;
using System.Collections.Generic;
using Orchard.Mvc;
using Orchard.Services;
using Orchard.UI.Resources;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public class CombinatorResourceManager : ICombinatorResourceManager
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJsonConverter _jsonConverter;


        public CombinatorResourceManager(IHttpContextAccessor httpContextAccessor, IJsonConverter jsonConverter)
        {
            _httpContextAccessor = httpContextAccessor;
            _jsonConverter = jsonConverter;
        }


        public CombinatorResource ResourceFactory(ResourceType type)
        {
            return new CombinatorResource(type, _httpContextAccessor);
        }


        public string SerializeResourceSettings(CombinatorResource resource)
        {
            var settings = resource.RequiredContext.Settings;
            if (settings == null) return "";

            return _jsonConverter.Serialize(
                new SerializableSettings()
                {
                    Url = resource.IsOriginal ? resource.AbsoluteUrl : null,
                    Culture = settings.Culture,
                    Condition = settings.Condition,
                    Attributes = settings.Attributes
                });
        }

        public void DeserializeSettings(string serialization, CombinatorResource resource)
        {
            if (String.IsNullOrEmpty(serialization)) return;

            var settings = _jsonConverter.Deserialize<SerializableSettings>(serialization);

            if (settings.Url != null)
            {
                resource.RequiredContext.Resource.SetUrlProtocolRelative(settings.Url);
                resource.IsOriginal = true;
            }

            if (resource.RequiredContext.Settings == null) resource.RequiredContext.Settings = new RequireSettings();
            var resourceSettings = resource.RequiredContext.Settings;
            resourceSettings.Culture = settings.Culture;
            resourceSettings.Condition = settings.Condition;
            resourceSettings.Attributes = settings.Attributes;
        }


        public class SerializableSettings
        {
            public Uri Url { get; set; }
            public string Culture { get; set; }
            public string Condition { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
        }
    }
}