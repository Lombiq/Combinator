using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Piedone.Combinator.SpriteGenerator.Utility
{
    internal class VerticalContour : Contour
    {
        /// <summary>
        /// Contour class for quick computation of x-coordinates during working with vertical O-Tree.
        /// </summary>
        /// <param name="root">First element of the contour.</param>
        public VerticalContour(Module root) : base(root)
        {
        }

        /// <summary>
        /// Finds the minimum x-coordinate where the module can be inserted.
        /// </summary>
        /// <param name="to">Maximum y-coordinate until modules on the left of the actual module need to be checked.</param>
        /// <returns></returns>
        public override int FindMax(int to)
        {
            int max = 0;
            //Actual module does not need to be checked.
            int indexFrom = _insertationIndex + 1;

            //Checking modules in contour.
            while (indexFrom < _moduleSequence.Count && _moduleSequence[indexFrom].Y < to)
            {
                //Overwriting maximum.
                if (max < _moduleSequence[indexFrom].X + _moduleSequence[indexFrom].Width)
                {
                    max = _moduleSequence[indexFrom].X + _moduleSequence[indexFrom].Width;
                    _whereMax = _moduleSequence[indexFrom];
                }

                //Removing modules, which are covered by the module will be inserted.
                if (_moduleSequence[indexFrom].Y + _moduleSequence[indexFrom].Height <= to)
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
