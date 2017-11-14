namespace SafetySharp.Bayesian
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class implementing the PC algorithm of Spirtes for learning the structure of a bayesian network given all conditional independencies
    /// </summary>
    class ConstraintBasedStructureLearner
    {

        /*
         * See Learning Bayesian Networks (Neapolitan), Algorithm 2
         * Begin with a complete skeleton
         * For every pair of nodes x and y:
         *     Are x and y independent given some set S?
         *     Then remove the edge between x and y and Sxy = Sxy union S.
         *     
         * Orient all uncoupled colliders: For every uncoupled link x - z - y:
         *      If z not in Sxy, then set x -> z <- y
         *      
         * Orient as many remaining edges as possible:     
         * repeat
         *      for each uncoupled meeting x -> z - y   =>   x -> z -> y.
         *      For x - y: if there is a directed path from x to y, then orient x -> y.
         *      If for each uncoupled meeting x - z - y, there is a w such that x -> w, y -> w and z - w, then orient z -> w.
         * until no more edges can be oriented
         * 
         */

        private readonly DagPattern<RandomVariable> _dag;
        private readonly IList<RandomVariable> _randomVariables;
        private readonly DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>> _independencies;
        private readonly DualKeyDictionary<RandomVariable, ISet<RandomVariable>> _collectedConditions;

        public ConstraintBasedStructureLearner(IList<RandomVariable> randomVariables, DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>> independencies)
        {
            _randomVariables = randomVariables;
            _independencies = independencies;
            _dag = DagPattern<RandomVariable>.InitCompleteDag(randomVariables);
            _collectedConditions = new DualKeyDictionary<RandomVariable, ISet<RandomVariable>>();
            foreach (var x in _dag.Nodes)
            {
                foreach (var y in _dag.Nodes.Where(y => x != y))
                {
                    _collectedConditions[x, y] = new HashSet<RandomVariable>();
                }
            }
        }

        /// <summary>
        /// Learn a DAG pattern given the conditional independencies
        /// </summary>
        public DagPattern<RandomVariable> LearnDag()
        {
            // See Learning Bayesian Networks (Neapolitan), Algorithm 2
            LearnSkeleton();
            Console.Out.WriteLine("DAG skeleton:");
            _dag.ExportToGraphviz();

            OrientUncoupledColliders();
            Console.Out.WriteLine("After marrying colliders:");
            _dag.ExportToGraphviz();

            OrientRemainingEdges(false);
            Console.Out.WriteLine("After orienting remaining edges:");
            _dag.ExportToGraphviz();

            return _dag;
        }

        /// <summary>
        /// Remove all edges x - y for x and y are independent given some set S.
        /// </summary>
        private void LearnSkeleton()
        {
            foreach (var tuple in _independencies)
            {
                _dag.RemoveEdge(tuple.Item1, tuple.Item2);
                _dag.RemoveEdge(tuple.Item2, tuple.Item1);
                foreach (var conditions in _independencies[tuple])
                {
                    _collectedConditions[tuple].UnionWith(conditions);
                    _collectedConditions[tuple.Item2, tuple.Item1] = _collectedConditions[tuple];
                }
            }
        }

        /// <summary>
        /// For every uncoupled link x - z - y:
        ///     If z not in Sxy, then set x -> z <- y
        /// </summary>
        private void OrientUncoupledColliders()
        {
            foreach (var x in _dag.Nodes)
            {
                for (var j = _dag.Nodes.IndexOf(x) + 1; j < _dag.Size; j++)
                {
                    var y = _dag.Nodes[j];
                    var undirectedBetweenXAndY = _dag.GetUndirectedNeighbors(x);
                    undirectedBetweenXAndY.IntersectWith(_dag.GetUndirectedNeighbors(y));
                    foreach (var node in undirectedBetweenXAndY.Where(node => !_collectedConditions[x, y].Contains(node) && IsUncoupledMeeting(x, node, y)))
                    {
                        _dag.OrientUndirectedEdge(x, node);
                        _dag.OrientUndirectedEdge(y, node);
                    }
                }
            }
        }

        /// <summary>
        /// repeat
        ///     for each uncoupled meeting x -> z - y   =>   x -> z -> y.
        ///     For x - y: if there is a directed path from x to y, then orient x -> y.
        ///     If for each uncoupled meeting x - z - y, there is a w such that x -> w, y -> w and z - w, then orient z -> w.
        ///     If step four is included:
        ///         for each X -> Y -> Z - W - X such that Y and W are linked and Z and X are not linked:
        ///             orient W - Z as W -> Z
        /// until no more edges can be oriented
        /// </summary>
        private void OrientRemainingEdges(bool includeStepFour)
        {
            bool edgeWasOriented;
            do
            {
                edgeWasOriented = false;
                var result = DoStepOne();
                if (result)
                    edgeWasOriented = true;
                result = DoStepTwo();
                if (result)
                    edgeWasOriented = true;
                result = DoStepThree();
                if (result)
                    edgeWasOriented = true;
                if (includeStepFour)
                {
                    result = DoStepFour();
                    if (result)
                        edgeWasOriented = true;
                }
            } while (edgeWasOriented);
        }

        /// <summary>
        /// for each uncoupled meeting x -> z - y   =>   x -> z -> y.
        /// </summary>
        /// <returns>If there was an edge that could be oriented</returns>
        private bool DoStepOne()
        {
            var somethingChanged = false;
            foreach (var x in _dag.Nodes)
            {
                // for every x -> z
                var directedNeighbors = _dag.GetDirectedChildren(x);
                foreach (var z in directedNeighbors)
                {
                    // for every x -> z - y
                    var undirectedNeighbors = _dag.GetUndirectedNeighbors(z);
                    foreach (var y in undirectedNeighbors)
                    {
                        //set z -> y  if x -> z - y uncoupled
                        if (IsUncoupledMeeting(x, z, y))
                        {
                            _dag.OrientUndirectedEdge(z, y);
                            Console.Out.WriteLine($"Changed {x.Name}->{z.Name}-{y.Name} to {x.Name}->{z.Name}->{y.Name}.");
                            somethingChanged = true;
                        }
                    }
                }
            }
            return somethingChanged;
        }

        /// <summary>
        /// For x - y: if there is a directed path from x to y, then orient x -> y
        /// </summary>
        /// <returns>If there was an edge that could be oriented</returns>
        private bool DoStepTwo()
        {
            var somethingChanged = false;
            foreach (var x in _dag.Nodes)
            {
                var undirectedNeighbors = _dag.GetUndirectedNeighbors(x);
                foreach (var y in undirectedNeighbors.Where(y => _dag.IsReachableWithDirectedEdges(x, y)))
                {
                    // set x - y to x -> y
                    _dag.OrientUndirectedEdge(x, y);
                    Console.Out.WriteLine(
                        $"Changed {x.Name}-{y.Name} to {x.Name}->{y.Name}, because there was a directed path from {x.Name} to {y.Name}.");
                    somethingChanged = true;
                }
            }
            return somethingChanged;
        }

        /// <summary>
        /// If for each uncoupled meeting x - z - y, there is a w such that x -> w, y -> w and z - w, then orient z -> w.
        /// </summary>
        /// <returns>If there was an edge that could be oriented</returns>
        private bool DoStepThree()
        {
            var somethingChanged = false;
            foreach (var x in _dag.Nodes)
            {
                var directedNeighborsX = _dag.GetDirectedChildren(x);
                var undirectedNeighborsX = _dag.GetUndirectedNeighbors(x);
                foreach (var z in undirectedNeighborsX)
                {
                    var undirectedNeighborsZ = _dag.GetUndirectedNeighbors(z);
                    // for each x - z - y
                    foreach (var y in undirectedNeighborsZ.Where(y => y != x))
                    {
                        var directedNeighborsY = _dag.GetDirectedChildren(y);
                        // only if x - z - y is uncoupled
                        if (!IsUncoupledMeeting(x, z, y))
                            continue;
                        // orient z -> w for all w with x -> w, y -> w, z - w
                        var toOrient = directedNeighborsX.Intersect(directedNeighborsY).Intersect(undirectedNeighborsZ);
                        foreach (var w in toOrient)
                        {
                            _dag.OrientUndirectedEdge(z, w);
                            somethingChanged = true;
                        }
                    }
                }
            }
            return somethingChanged;
        }
        /// <summary>
        /// for each X -> Y -> Z - W - X such that Y and W are linked and Z and X are not linked:
        ///     orient W - Z as W -> Z
        /// </summary>
        /// <returns></returns>
        private bool DoStepFour()
        {
            var somethingOriented = false;
            foreach (var x in _dag.Nodes)
            {
                var childrenOfX = _dag.GetDirectedChildren(x);
                var undirectedNeighborsOfX = _dag.GetUndirectedNeighbors(x);
                foreach (var y in childrenOfX)
                {
                    var childrenOfY = _dag.GetDirectedChildren(y);
                    foreach (var z in childrenOfY.Where(z => !_dag.AreAdjecent(z, x)))
                    {
                        var undirectedNeighborsOfZ = _dag.GetUndirectedNeighbors(z);
                        var betweenZAndX = undirectedNeighborsOfZ.Intersect(undirectedNeighborsOfX).ToList();
                        foreach (var w in betweenZAndX.Where(w => _dag.AreAdjecent(w, y)))
                        {
                            _dag.OrientUndirectedEdge(w, z);
                            somethingOriented = true;
                        }
                    }
                }
            }
            return somethingOriented;
        }

        /// <summary>
        /// Returns true if there is a meeting left - middle - right and left and right are not adjecent
        /// </summary>
        private bool IsUncoupledMeeting(RandomVariable left, RandomVariable middle, RandomVariable right)
        {
            return _dag.AreAdjecent(left, middle) && _dag.AreAdjecent(middle, right) && !_dag.AreAdjecent(left, right);
        }

        /// <summary>
        /// Use the structure of the DCCA to improve the missing orientations in the current DAG pattern.
        /// </summary>
        public DagPattern<RandomVariable> UseDccaForOrientations(ICollection<McsRandomVariable> minimalCriticalSets, ICollection<FaultRandomVariable> faults, BooleanRandomVariable hazard)
        {
            foreach (var minimalCriticalSet in minimalCriticalSets)
            {
                _dag.OrientUndirectedEdge(minimalCriticalSet, hazard);
                foreach (var fault in minimalCriticalSet.FaultVariables)
                {
                    _dag.OrientUndirectedEdge(fault, minimalCriticalSet);
                }
            }
            Console.Out.WriteLine("After using DCCA to orient edges:");
            _dag.ExportToGraphviz();
            OrientRemainingEdges(false);
            Console.Out.WriteLine("After orienting remaining edges after DCCA:");
            _dag.ExportToGraphviz();

            return _dag;
        }

        /// <summary>
        /// Create a DAG from the current DAG pattern by using the chronological order of the fault list and the usual orienting algorithm.
        /// </summary>
        public DagPattern<RandomVariable> DagPatternToDag()
        {
            // see Learning Bayesian Networks (Neapolitan), Algorithm 3 (with different step numbers)
            /*
             * while there are unoriented edges:
             *      choose and unoriented edge X - Y and orient it as X -> Y
             *      while more edges can be oriented
             *          Do Step one, two, three and four
             */
            foreach (var randomVariable in _randomVariables)
            {
                var undirectedNeighbors = _dag.GetUndirectedNeighbors(randomVariable);
                foreach (var undirectedNeighbor in undirectedNeighbors)
                {
                    _dag.OrientUndirectedEdge(randomVariable, undirectedNeighbor);
                    OrientRemainingEdges(true);
                }
            }
            Console.Out.WriteLine("After DagPatternToDag:");
            _dag.ExportToGraphviz();
            return _dag;
        }

        public void ExportToGraphviz()
        {
            _dag.ExportToGraphviz();
        }
    }


}
