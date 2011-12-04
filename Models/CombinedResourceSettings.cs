using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using Orchard.UI.Resources;
using System.Xml;

namespace Piedone.Combinator.Models
{
    /// <summary>
    /// Settings that are persisted with a combined resource file
    /// </summary>
    /// <remarks>
    /// Should really be of internal visibility but even with DataContractSerializer it's not possible to serialize
    /// non-public types in medium trust.
    /// </remarks>
    public class CombinedResourceSettings
    {
        public string PublicUrl { get; set; }
        public string Culture { get; set; }
        public string Condition { get; set; }

        public static CombinedResourceSettings Factory(ResourceRequiredContext resource)
        {
            var settings = resource.Settings;

            return new CombinedResourceSettings()
            {
                Culture = settings.Culture,
                Condition = settings.Condition
            };
        }

        public static CombinedResourceSettings Factory(string serialization)
        {
            var doc = new XmlDocument();
            doc.LoadXml(serialization);
            var reader = new XmlNodeReader(doc.DocumentElement);
            var serializer = new XmlSerializer(typeof(CombinedResourceSettings));
            return (CombinedResourceSettings)serializer.Deserialize(reader);
        }

        public string GetSerialization()
        {
            var serializer = new XmlSerializer(this.GetType());
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            serializer.Serialize(sw, this);
            sw.Close();
            return sb.ToString();
        }

        public void TranscribeSettings(ResourceRequiredContext resource)
        {
            var settings = resource.Settings;

            settings.Culture = Culture;
            settings.Condition = Condition;
        }
    }
}