namespace SafetySharp.Bayesian
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class for DAGs and DAG patterns consisting of generic nodes. Supports directed and undirected edges, 
    /// where an undirected edge A - B is considered as equivalent to A -> B and B -> A.
    /// </summary>
    public class DagPattern<T>
    {
        private readonly int[,] _edges;
        public IList<T> Nodes { get; }
        public int Size => Nodes.Count;

        /// <summary>
        /// Creates a complete dag where every random variable is a node
        /// </summary>
        public static DagPattern<T> InitCompleteDag(IList<T> nodes)
        {
            return new DagPattern<T>(nodes, 1);
        }

        public static DagPattern<T> InitEmptyDag(IList<T> nodes)
        {
            return new DagPattern<T>(nodes, 0);
        }

        public static DagPattern<T> InitDagWithMatrix(IList<T> nodes, int[,] adjacencyMatrix)
        {
            return new DagPattern<T>(nodes, adjacencyMatrix);
        }

        private DagPattern(IList<T> nodes, int initValue)
        {
            Nodes = nodes;
            _edges = new int[Nodes.Count, Nodes.Count];
            for (var i = 0; i < Nodes.Count; i++)
            {
                for (var j = 0; j < Nodes.Count; j++)
                {
                    if (i != j)
                    {
                        _edges[i, j] = initValue;
                    }
                    else
                    {
                        _edges[i, j] = 0;
                    }
                }
            }
        }

        private DagPattern(IList<T> nodes, int[,] adjacencyMatrix)
        {
            Nodes = nodes;
            _edges = adjacencyMatrix;
        }

        /// <summary>
        /// Adds the edge A -> B if it does not exist yet.
        /// </summary>
        public void AddEdge(T a, T b)
        {
            _edges[Nodes.IndexOf(a), Nodes.IndexOf(b)] = 1;
        }

        /// <summary>
        /// Removes the edge A -> B if it exists
        /// </summary>
        public void RemoveEdge(T a, T b)
        {
            _edges[Nodes.IndexOf(a), Nodes.IndexOf(b)] = 0;
        }

        /// <summary>
        /// If an undirected edge A - B exists, then it orients it to A -> B
        /// </summary>
        public void OrientUndirectedEdge(T a, T b)
        {
            var nodeA = Nodes.IndexOf(a);
            var nodeB = Nodes.IndexOf(b);
            if (IsUndirectedEdge(nodeA, nodeB))
            {
                _edges[nodeB, nodeA] = 0;
            }
            else
            {
                if (IsDirectedEdge(nodeA, nodeB))
                    Console.Out.WriteLine($"The edge {a} -> {b} was already oriented!");
                else if (IsDirectedEdge(nodeB, nodeA))
                    Console.Out.WriteLine($"There edge {b} -> {a} was already oriented the other way!");
                else
                    Console.Out.WriteLine($"There was no edge at all between {a} and {b} that could be oriented!");
            }
        }

        public bool AreAdjecent(T a, T b)
        {
            var nodeA = Nodes.IndexOf(a);
            var nodeB = Nodes.IndexOf(b);
            return IsUndirectedEdge(nodeA, nodeB) || IsDirectedEdge(nodeA, nodeB) || IsDirectedEdge(nodeB, nodeA);
        }

        private bool IsUndirectedEdge(int a, int b)
        {
            return _edges[a, b] > 0 && _edges[b, a] > 0;
        }

        /// <summary>
        /// Returns if the DAG pattern contains a directed edge a -> b
        /// </summary>
        public bool IsDirectedEdge(T a, T b)
        {
            var nodeA = Nodes.IndexOf(a);
            var nodeB = Nodes.IndexOf(b);
            return IsDirectedEdge(nodeA, nodeB);
        }

        private bool IsDirectedEdge(int a, int b)
        {
            return _edges[a, b] > 0 && _edges[b, a] == 0;
        }

        /// <summary>
        /// Returns all children of a node A. Children in this context are all nodes B, where an edge A -> B exists.
        /// The edge can also be undirected, so if there are edges A -> B and B -> A, B is a child of A.
        /// </summary>
        public ISet<T> GetChildren(T node)
        {
            var nodeIndex = Nodes.IndexOf(node);
            var children = new HashSet<int>();
            var row = Row(nodeIndex);
            for (var i = 0; i < row.Length; i++)
            {
                if (row[i] > 0 && i != nodeIndex)
                {
                    children.Add(i);
                }
            }
            return new HashSet<T>(children.Select(index => Nodes[index]));
        }

        /// <summary>
        /// Returns all 'real'/directed children of a node A, i.e. all nodes B, where an edge A -> B exists, but no edge B -> A.
        /// </summary>
        public ISet<T> GetDirectedChildren(T node)
        {
            var children = GetChildren(node);
            var parents = GetParents(node);
            return new HashSet<T>(children.Except(parents));
        }

        /// <summary>
        /// Returns all parents of a node A. Parents in this context are all nodes B, where an edge B -> A exists.
        /// The edge can also be undirected, so if there are edges A -> B and B -> A, B is a parent of A.
        /// </summary>
        public ISet<T> GetParents(T node)
        {
            var nodeIndex = Nodes.IndexOf(node);
            var parents = new HashSet<int>();
            var column = Column(nodeIndex);
            for (var i = 0; i < column.Length; i++)
            {
                if (column[i] > 0 && i != nodeIndex)
                {
                    parents.Add(i);
                }
            }
            return new HashSet<T>(parents.Select(index => Nodes[index]));
        }
        /// <summary>
        /// Returns all 'real'/directed parents of a node A, i.e. all nodes B, where an edge A -> B exists, but no edge B -> A.
        /// </summary>
        public ISet<T> GetDirectedParents(T node)
        {
            var parents = GetParents(node);
            var children = GetChildren(node);
            return new HashSet<T>(parents.Except(children));
        }

        /// <summary>
        /// Returns all undirected neighbors of a node A, i.e. all nodes B, where an undirected edge A - B exists.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public ISet<T> GetUndirectedNeighbors(T node)
        {
            var fromNode = GetChildren(node);
            var toNode = GetParents(node);
            return new HashSet<T>(fromNode.Intersect(toNode));
        }

        /// <summary>
        /// Returns true, if the node 'goal' is reachable from the node 'node' limited by using directed edges, i.e. disregarding undirected edges.
        /// </summary>
        public bool IsReachableWithDirectedEdges(T node, T goal)
        {
            var nodeIndex = Nodes.IndexOf(node);
            var goalIndex = Nodes.IndexOf(goal);

            var discovered = new bool[Nodes.Count];
            var stack = new Stack<int>();
            stack.Push(nodeIndex);
            while (stack.Count > 0)
            {
                var currentNode = stack.Pop();
                if (currentNode == goalIndex)
                {
                    return true;
                }
                if (!discovered[currentNode])
                {
                    discovered[currentNode] = true;
                    var neighbors = GetDirectedChildren(Nodes[currentNode]);
                    foreach (var neighborNode in neighbors)
                    {
                        // use only directed edges
                        var neighborNodeIndex = Nodes.IndexOf(neighborNode);
                        stack.Push(neighborNodeIndex);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the DAG pattern is actually a DAG, i.e. it contains no cycles and only has directed edges.
        /// </summary>
        public bool IsDag()
        {
            var visited = new bool[Nodes.Count];
            var finished = new bool[Nodes.Count];
            foreach (var nodeIndex in Nodes.Select(node => Nodes.IndexOf(node)))
            {
                var cycle = CycleDfs(nodeIndex, finished, visited);
                if (cycle)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Modified DFS to search for cycles in the graph.
        /// </summary>
        /// <param name="node">the node to start the search from</param>
        /// <param name="finished">nodes that were already completely processed</param>
        /// <param name="visited">nodes that were already visited</param>
        /// <returns>True if there was a cycle, else false</returns>
        private bool CycleDfs(int node, IList<bool> finished, IList<bool> visited)
        {
            if (finished[node])
            {
                return false;
            }
            if (visited[node])
            {
                return true;
            }
            visited[node] = true;
            var children = GetChildren(Nodes[node]);
            foreach (var child in children)
            {
                var cycle = CycleDfs(Nodes.IndexOf(child), finished, visited);
                if (cycle)
                {
                    return true;
                }
            }
            finished[node] = true;
            return false;
        }

        /// <summary>
        /// Exports the DAG to a graphviz digraph.
        /// </summary>
        public void ExportToGraphviz()
        {
            Console.Out.WriteLine("digraph S {");
            for (var i = 0; i < Nodes.Count; i++)
            {
                Console.Out.WriteLine($"{i} [label=\"{Nodes[i]}\"];");
            }
            for (var i = 0; i < Nodes.Count; i++)
            {
                for (var j = i + 1; j < Nodes.Count; j++)
                {
                    if (_edges[i, j] > 0 && _edges[j, i] > 0)
                    {
                        Console.Out.WriteLine($"{i} -> {j} [dir=\"none\"];");
                    }
                    else if (_edges[i, j] > 0)
                    {
                        Console.Out.WriteLine($"{i} -> {j};");
                    }
                    else if (_edges[j, i] > 0)
                    {
                        Console.Out.WriteLine($"{j} -> {i};");
                    }
                }
            }

            Console.Out.WriteLine("}");
        }

        /// <summary>
        /// Returns true, if the given matrix has the same edges as this DAG.
        /// </summary>
        public bool EqualsAdjacencyMatrix(int[,] matrix)
        {
            if (matrix.Rank != 2 || matrix.GetLength(0) != _edges.GetLength(0) || matrix.GetLength(1) != _edges.GetLength(1))
            {
                return false;
            }
            for (var i = 0; i < matrix.GetLength(0); i++)
            {
                for (var j = 0; j < matrix.GetLength(1); j++)
                {
                    if (matrix[i, j] != _edges[i, j])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private int[] Row(int rowIndex)
        {
            var row = new int[_edges.GetLength(1)];
            for (var i = 0; i < _edges.GetLength(1); i++)
            {
                row[i] = _edges[rowIndex, i];
            }
            return row;
        }

        private int[] Column(int columnIndex)
        {
            var column = new int[_edges.GetLength(0)];
            for (var i = 0; i < _edges.GetLength(0); i++)
            {
                column[i] = _edges[i, columnIndex];
            }
            return column;
        }
    }
}
