using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.ContentManagement.Records;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinatorSettingsPartRecord : ContentPartRecord
    {
        public virtual bool CombineCDNResources { get; set; }

        public CombinatorSettingsPartRecord()
        {
            CombineCDNResources = false;
        }
    }
}