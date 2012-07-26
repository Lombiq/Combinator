using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Piedone.Combinator.SpriteGenerator.Utility
{
    internal class GraphNode
    {
        public Dictionary<int, int> IncomingEdges { get; set; }
        public Dictionary<int, int> OutgoingEdges { get; set; }

        public void InitializeEdges()
        {
            IncomingEdges = new Dictionary<int, int>();
            OutgoingEdges = new Dictionary<int, int>();
        }
    }
}
