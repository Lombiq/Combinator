using System.Text;
using Orchard;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    /// <summary>
    /// Service for processing static resources during the Combinator resource processing pipeline.
    /// </summary>
    public interface IResourceProcessingService : IDependency
    {
        void ProcessResource(CombinatorResource resource, StringBuilder combinedContent, ICombinatorSettings settings);
        void ReplaceCssImagesWithSprite(CombinatorResource resource);
    }
}
