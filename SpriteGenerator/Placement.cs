using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Piedone.Combinator.SpriteGenerator.Utility;

namespace Piedone.Combinator.SpriteGenerator
{
    internal class Placement
    {
        private List<Module> _modules;

        public Placement(List<Module> modules)
        {
            _modules = modules;
        }

        /// <summary>
        /// Gets the half perimeter of the placement.
        /// </summary>
        public int Perimeter
        {
            get { return _modules.Max(m => m.X + m.Width) + _modules.Max(m => m.Y + m.Height); }
        }

        /// <summary>
        /// Gets the width of the palcement.
        /// </summary>
        public int Width
        {
            get { return _modules.Max(m => m.X + m.Width); }
        }

        /// <summary>
        /// Gets the height of the placement.
        /// </summary>
        public int Height
        {
            get { return _modules.Max(m => m.Y + m.Height); }
        }

        /// <summary>
        /// Gets the modules in the placement.
        /// </summary>
        public List<Module> Modules
        {
            get { return _modules; }
        }
    }
}
