using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Piedone.Combinator.SpriteGenerator
{
    internal class LayoutProperties
    {
        public string[] inputFilePaths; 
        public string outputSpriteFilePath;
        public string outputCssFilePath;

        public LayoutProperties()
        {
            inputFilePaths = null;
            outputSpriteFilePath = "";
            outputCssFilePath = "";
        }
    }
}
