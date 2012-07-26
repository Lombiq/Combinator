using System.Text;
using Orchard;
using Piedone.Combinator.Models;
using System.Text.RegularExpressions;

namespace Piedone.Combinator.Services
{
    public interface IResourceProcessingService : IDependency
    {
        void ProcessResource(CombinatorResource resource, StringBuilder combinedContent, ICombinatorSettings settings);
        void ReplaceCssImagesWithSprite(CombinatorResource resource);
    }
}
