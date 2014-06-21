using Orchard;
using Piedone.Combinator.Models;

namespace Piedone.Combinator.Services
{
    /// <summary>
    /// Service for managing and editing <see cref="Piedone.Combinator.Models.CombinatorResource"/> objects.
    /// </summary>
    public interface ICombinatorResourceManager :  IDependency
    {
        CombinatorResource ResourceFactory(ResourceType type);
        void DeserializeSettings(string serialization, CombinatorResource resource);
        string SerializeResourceSettings(CombinatorResource resource);
    }
}
