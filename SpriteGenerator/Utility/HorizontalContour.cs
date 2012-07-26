using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Piedone.Combinator.SpriteGenerator.Utility
{
    internal class HorizontalContour : Contour
    {
        /// <summary>
        /// Contour class for quick computation of y-coordinates during working with horizontal O-Tree.
        /// </summary>
        /// <param name="root">First element of the contour.</param>
        public HorizontalContour(Module root) : base(root)
        {
        }

        /// <summary>
        /// Finds the minimum y-coordinate where the module can be inserted.
        /// </summary>
        /// <param name="to">Maximum x-coordinate until modules below the actual module need to be checked.</param>
        /// <returns></returns>
        public override int FindMax(int to)
        {
            int max = 0;
            //Actual module does not need to be checked.
            int indexFrom = _insertationIndex + 1;

            //Checking modules in contour.
            while (indexFrom < _moduleSequence.Count && _moduleSequence[indexFrom].X < to)
            {
                //Overwriting maximum.
                if (max < _moduleSequence[indexFrom].Y + _moduleSequence[indexFrom].Height)
                {
                    max = _moduleSequence[indexFrom].Y + _moduleSequence[indexFrom].Height;
                    _whereMax = _moduleSequence[indexFrom];
                }

                //Removing modules, which are covered by the module will be isnserted.
                if (_moduleSequence[indexFrom].X + _moduleSequence[indexFrom].Width <= to)
                {
                    _moduleSequence.RemoveAt(indexFrom);
                }

                else
                    indexFrom++;
            }

            return max;
        }
    }
}
