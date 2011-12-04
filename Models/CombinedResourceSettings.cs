using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using Orchard.UI.Resources;
using System.Xml;
using System.Runtime.Serialization;

namespace Piedone.Combinator.Models
{
    /// <summary>
    /// Settings that are persisted with a combined resource file
    /// </summary>
    /// <remarks>
    /// Should really be of internal visibility but it's not possible to serialize non-public types in medium trust.
    /// </remarks>
    [DataContract]
    public class CombinedResourceSettings
    {
        [DataMember]
        public string PublicUrl { get; set; }

        [DataMember]
        public string Culture { get; set; }

        [DataMember]
        public string Condition { get; set; }

        [DataMember]
        public Dictionary<string, string> Attributes { get; set; }

        public static CombinedResourceSettings Factory(string serialization)
        {
            var serializer = new DataContractSerializer(typeof(CombinedResourceSettings));
            var doc = new XmlDocument();
            doc.LoadXml(serialization);
            var reader = new XmlNodeReader(doc.DocumentElement);
            return (CombinedResourceSettings)serializer.ReadObject(reader);
        }

        public string GetSerialization()
        {
            string xmlString;

            using (var sw = new StringWriter())
            {
                using (var writer = new XmlTextWriter(sw))
                {
                    var serializer = new DataContractSerializer(this.GetType());
                    //writer.Formatting = Formatting.Indented; // indent the Xml so it's human readable
                    serializer.WriteObject(writer, this);
                    writer.Flush();
                    xmlString = sw.ToString();
                }
            }

            return xmlString;
        }

        public void TranscribeFromRequiredContext(ResourceRequiredContext context)
        {
            if (context == null) return;

            var settings = context.Settings;

            Culture = settings.Culture;
            Condition = settings.Condition;
            if (settings.HasAttributes) Attributes = settings.Attributes;
        }

        public void TranscribeToRequiredContext(ResourceRequiredContext context)
        {
            if (context == null) return;

            var settings = context.Settings;

            settings.Culture = Culture;
            settings.Condition = Condition;
            settings.Attributes = Attributes;
        }
    }
}