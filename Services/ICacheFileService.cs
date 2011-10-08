using System;
using Piedone.Combinator.Helpers;
using System.Collections.Generic;
using Orchard;

namespace Piedone.Combinator.Services
{
    public interface ICacheFileService : IDependency
    {
        string Save(int hashCode, ResourceType type, string content);
        List<string> GetPublicUrls(int hashCode);
        bool Exists(int hashCode);
        void Delete(int hashCode, ResourceType type);
        void Truncate();
    }
}
