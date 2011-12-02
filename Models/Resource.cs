using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.UI.Resources;
using Piedone.Combinator.Extensions;

namespace Piedone.Combinator.Models
{
    internal class Resource
    {
        public ResourceRequiredContext ResourceRequiredContext { get; set; }

        public Resource(ResourceRequiredContext resource)
        {
            ResourceRequiredContext = resource;
        }

        public string FullPath
        {
            get { return ResourceRequiredContext.Resource.GetFullPath(); }
        }

        public bool IsCDNResource
        {
            get { return ResourceRequiredContext.Resource.IsCDNResource(); }
        }

        public bool IsConditional
        {
            get { return !String.IsNullOrEmpty(ResourceRequiredContext.Settings.Condition); }
        }
        
    }
}