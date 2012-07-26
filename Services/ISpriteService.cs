using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Orchard;

namespace Piedone.Combinator.Services
{
    public interface ISpriteService : IDependency
    {
        string ReplaceImagesWithSprite(string css);
    }
}
