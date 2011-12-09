using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Piedone.Combinator.Models
{
    public class CombinatorSettings : ICombinatorSettings
    {
        public string CombinationExcludeRegex { get; set; }
        public bool CombineCDNResources { get; set; }
        public bool MinifyResources { get; set; }
        public string MinificationExcludeRegex { get; set; }
    }
}