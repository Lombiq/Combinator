using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Piedone.Combinator.SpriteGenerator.Utility
{
    //Contour is the list of the modules on the top (horizontal contour) or on the right (vertical contour) of the 
    //placement. It is needed for linear time computation of the modules coordinates. It is easier to understand 
    //it from some figure. See reference.
    internal abstract class Contour
    {
        protected List<Module> _moduleSequence;
        protected int _insertationIndex;
        protected Module _whereMax;

        protected Contour(Module root)
        {
            _moduleSequence = new List<Module>();
            _moduleSequence.Add(root);
            _whereMax = root;
            _insertationIndex = -1;
        }

        /// <summary>
        /// Sets the insertation index of the contour.
        /// </summary>
        public int InsertationIndex
        {
            set { _insertationIndex = value; }
        }

        /// <summary>
        /// Gets the sequence of modules whereof the contour consists.
        /// </summary>
        public List<Module> ModuleSequence
        {
            get { return _moduleSequence; }
        }

        /// <summary>
        /// Gets the module with the maximum y-coordinate in a given x-coordinate range or conversely.
        /// It is calculated by FindMax method.
        /// </summary>
        public Module WhereMax
        {
            get { return _whereMax; }
        }

        public abstract int FindMax(int to);

        /// <summary>
        /// Inserts new module into the contour and clears WhereMax value.
        /// </summary>
        /// <param name="module"></param>
        public void Update(Module module)
        {
            _moduleSequence.Insert(++_insertationIndex, module);
            _whereMax = new Module(-1, null);
        }
    }
}
