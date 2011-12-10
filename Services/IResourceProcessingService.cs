using System.Text;
using Orchard;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    public interface IResourceProcessingService : IDependency
    {
        void ProcessResource(ISmartResource resource, StringBuilder combinedContent, ICombinatorSettings settings);
    }
}
