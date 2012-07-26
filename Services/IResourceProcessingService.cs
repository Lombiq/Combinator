using System.Text;
using Orchard;
using Piedone.Combinator.Models;
using System.Text.RegularExpressions;

namespace Piedone.Combinator.Services
{
    public delegate string ImageMatchProcessor(string url, string extension, Match match);

    public interface IResourceProcessingService : IDependency
    {
        void ProcessResource(CombinatorResource resource, StringBuilder combinedContent, ICombinatorSettings settings);
        void ProcessImages(CombinatorResource resource, ImageMatchProcessor matchProcessor);
    }
}
