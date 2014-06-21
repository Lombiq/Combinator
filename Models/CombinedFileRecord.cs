using System;
using Orchard.Data.Conventions;
using Orchard.Environment.Extensions;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class CombinedFileRecord
    {
        public virtual int Id { get; set; }
        public virtual int HashCode { get; set; }
        public virtual string Fingerprint { get; set; }
        public virtual int Slice { get; set; }
        public virtual ResourceType Type { get; set; }
        public virtual DateTime? LastUpdatedUtc { get; set; }
        [StringLengthMax]
        public virtual string Settings { get; set; }


        public CombinedFileRecord()
        {
            HashCode = 9; // Just to prevent NULL errors while keeping DB schema backwards compatible.
        }
    }
}