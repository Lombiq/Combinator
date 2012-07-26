using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Piedone.Combinator.SpriteGenerator.Utility
{
    internal class Graph
    {
        private Dictionary<int, GraphNode> _nodes;

        /// <summary>
        /// Directed graph with weigted edges.
        /// </summary>
        /// <param name="_nodes">List of node labels.</param>
        public Graph(List<int> nodes)
        {
            //Nodes of the graph.
            _nodes = new Dictionary<int, GraphNode>();

            //Initializing list of edges for all nodes.
            foreach (int node in nodes)
            {
                GraphNode gn = new GraphNode();
                gn.InitializeEdges();
                _nodes.Add(node, gn);
            }
        }

        /// <summary>
        /// Adds edge to the graph.
        /// </summary>
        /// <param name="from">Source node of the edge.</param>
        /// <param name="to">Sink node of the edge.</param>
        /// <param name="weight">Weight of the edge.</param>
        public void AddEdge(int from, int to, int weight)
        {
            _nodes[from].OutgoingEdges.Add(to, weight);
            _nodes[to].IncomingEdges.Add(from, weight);
        }

        /// <summary>
        /// Ordered visit, neighbors with lower weight have higher preference. If the graph is horizontal, weight 
        /// means x-coordinate of the module, which is represented by the sink node of the edge. If the graph is 
        /// vetrical, weight means the y-coordinate.
        /// </summary>
        /// <param name="node">Actual node.</param>
        /// <param name="visitedNodes">Visited nodes.</param>
        /// <param name="topologicalOrder">Topological order of the graph.</param>
        /// <param name="dfsSequence">0-1 sequence representing of the DFS traversing. 0 means forth step,
        /// 1 means back step.</param>
        private void VisitNode(int node, Dictionary<int, bool> visitedNodes, List<int> topologicalOrder, List<Bit> dfsSequence)
        {
            //If node is not visited yet
            if (!visitedNodes[node])
            {
                visitedNodes[node] = true;

                //Adding node to topological order and adding a 0 to DFS sequense strores the forth step belongs 
                //to the module
                topologicalOrder.Add(node);
                dfsSequence.Add(0);

                //Ordering neighbors by their weights
                var orderedNeighbors = from neighbor in _nodes[node].OutgoingEdges.Keys
                                       orderby _nodes[node].OutgoingEdges[neighbor]
                                       select neighbor;

                //Ordered list of neighbors
                List<int> neighborsOfNode = new List<int>(orderedNeighbors);

                //Visiting nodes in the afore calculated order.
                foreach (int neighbor in neighborsOfNode)
                {
                    VisitNode(neighbor, visitedNodes, topologicalOrder, dfsSequence);
                }
            }

            //If node and all of its DFS descendants are visited, it's needed to store the back step 
            //belonging to the module.
            dfsSequence.Add(1);
        }

        /// <summary>
        /// Ordered depth first search. Calculates module sequence and DFS sequence belonging to the graph. 
        /// These determines the O-Tree with the same orientation what the graph has.
        /// </summary>
        /// <param name="dfsSequence">O-Tree DFSSequence.</param>
        /// <returns>O-Tree ModuleSequence.</returns>
        public List<int> DepthFirstSearch(List<Bit> dfsSequence)
        {
            //Clearing DFS sequence and creating an empty list of module labels.
            dfsSequence.Clear();
            List<int> nodeOrder = new List<int>();
            //Every node is unvisited yet.
            Dictionary<int, bool> visitedNodes = _nodes.Keys.ToDictionary(item => item, item => false);

            //Visit starts at root module representing by label -1.
            VisitNode(-1, visitedNodes, nodeOrder, dfsSequence);

            //Removing root module.
            nodeOrder.RemoveAt(0);
            dfsSequence.RemoveAt(0);
            dfsSequence.RemoveAt(dfsSequence.Count - 1);

            return nodeOrder;
        }
    }
}
