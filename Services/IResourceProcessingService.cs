using System;
using Piedone.Combinator.Models;
using System.Text;
using Orchard;

namespace Piedone.Combinator.Services
{
    public interface IResourceProcessingService : IDependency
    {
        void ProcessResource(ISmartResource resource, StringBuilder combinedContent, ICombinatorSettings settings);
    }
}
