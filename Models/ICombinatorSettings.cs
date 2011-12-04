using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Piedone.Combinator.Models
{
    public interface ICombinatorSettings
    {
        string CombinationExcludeRegex { get; }
        bool CombineCDNResources { get; }
        bool MinifyResources { get; }
        string MinificationExcludeRegex { get; }
    }
}
