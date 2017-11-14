namespace Tests.Bayesian
{
    using System.Linq;
    using SafetySharp.Bayesian;
    using Xunit;

    public class DagPatternTests
    {

        [Fact]
        public void TestEmptyDag()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var dag = DagPattern<string>.InitEmptyDag(new[] { n1, n2, n3, n4 });
            var expectedMatrix = new[,]
            {
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 },
                { 0, 0, 0, 0 }
            };
            Assert.True(dag.EqualsAdjacencyMatrix(expectedMatrix));
        }

        [Fact]
        public void TestCompleteDag()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var dag = DagPattern<string>.InitCompleteDag(new[] { n1, n2, n3, n4 });
            var expectedMatrix = new[,]
            {
                { 0, 1, 1, 1 },
                { 1, 0, 1, 1 },
                { 1, 1, 0, 1 },
                { 1, 1, 1, 0 }
            };
            Assert.True(dag.EqualsAdjacencyMatrix(expectedMatrix));
        }

        [Fact]
        public void TestDagFromMatrix()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var matrix = new[,]
            {
                { 0, 0, 1, 1 },
                { 1, 0, 0, 1 },
                { 1, 0, 0, 0 },
                { 0, 1, 1, 0 }
            };
            var dag = DagPattern<string>.InitDagWithMatrix(new[] { n1, n2, n3, n4 }, matrix);
            Assert.True(dag.EqualsAdjacencyMatrix(matrix));
        }

        [Fact]
        public void TestRemoving()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var matrix = new[,]
            {
                { 0, 0, 1, 1 },
                { 1, 0, 0, 1 },
                { 1, 0, 0, 0 },
                { 0, 1, 1, 0 }
            };
            var dag = DagPattern<string>.InitDagWithMatrix(new[] { n1, n2, n3, n4 }, matrix);
            var modifiedMatrix = (int[,])matrix.Clone();
            dag.RemoveEdge(n1, n4);
            modifiedMatrix[0, 3] = 0;
            dag.RemoveEdge(n2, n1);
            modifiedMatrix[1, 0] = 0;

            Assert.True(dag.EqualsAdjacencyMatrix(modifiedMatrix));
        }

        [Fact]
        public void TestOrienting()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var matrix = new[,]
            {
                { 0, 0, 1, 1 },
                { 1, 0, 0, 1 },
                { 1, 0, 0, 0 },
                { 0, 1, 1, 0 }
            };
            var dag = DagPattern<string>.InitDagWithMatrix(new[] { n1, n2, n3, n4 }, matrix);
            var modifiedMatrix = (int[,])matrix.Clone();

            dag.OrientUndirectedEdge(n2, n1); // n2 -> n1 is not directed
            dag.OrientUndirectedEdge(n2, n3); // n2 -> n3 does not exist
            dag.OrientUndirectedEdge(n1, n3); // n1 - n3 gets n1 -> n3
            modifiedMatrix[2, 0] = 0;
            dag.OrientUndirectedEdge(n4, n2); // n4 - n2 gets n4 -> n2
            modifiedMatrix[1, 3] = 0;

            Assert.True(dag.EqualsAdjacencyMatrix(modifiedMatrix));
        }

        [Fact]
        public void TestNodeRelations()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var matrix = new[,]
               {
                { 0, 0, 1, 1 },
                { 1, 0, 0, 1 },
                { 1, 0, 0, 1 },
                { 0, 1, 1, 0 }
            };
            var dag = DagPattern<string>.InitDagWithMatrix(new[] { n1, n2, n3, n4 }, matrix);

            var expectedChildren = new[] { n3, n4 };
            var children = dag.GetChildren(n1);
            Assert.True(!expectedChildren.Except(children).Any() && expectedChildren.Length == children.Count);

            var expectedRealChildren = new[] { n4 };
            var realChildren = dag.GetDirectedChildren(n1);
            Assert.True(!expectedRealChildren.Except(realChildren).Any() && expectedRealChildren.Length == realChildren.Count);

            var expectedParents = new[] { n2, n3 };
            var parents = dag.GetParents(n1);
            Assert.True(!expectedParents.Except(parents).Any() && expectedParents.Length == parents.Count);

            var expectedRealParents = new[] { n2 };
            var realParents = dag.GetDirectedParents(n1);
            Assert.True(!expectedRealParents.Except(realParents).Any() && expectedRealParents.Length == realParents.Count);

            var expectedUndirectedNeighbors = new[] { n1, n4 };
            var undirectedNeighbors = dag.GetUndirectedNeighbors(n3);
            Assert.True(!expectedUndirectedNeighbors.Except(undirectedNeighbors).Any() && expectedUndirectedNeighbors.Length == undirectedNeighbors.Count);
        }

        [Fact]
        public void TestIsDagCycle()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var matrix = new[,]
            {
                { 0, 1, 0, 0 },
                { 0, 0, 1, 0 },
                { 1, 0, 0, 1 },
                { 0, 0, 0, 0 }
            };
            var dag = DagPattern<string>.InitDagWithMatrix(new[] { n1, n2, n3, n4 }, matrix);
            Assert.False(dag.IsDag());
        }

        [Fact]
        public void TestIsDagUndirectedEdge()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var matrix = new[,]
            {
                { 0, 1, 0, 0 },
                { 1, 0, 1, 0 },
                { 0, 0, 0, 1 },
                { 0, 0, 0, 0 }
            };
            var dag = DagPattern<string>.InitDagWithMatrix(new[] { n1, n2, n3, n4 }, matrix);
            Assert.False(dag.IsDag());
        }

        [Fact]
        public void TestIsDag()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var matrix = new[,]
            {
                { 0, 1, 0, 1 },
                { 0, 0, 1, 1 },
                { 0, 0, 0, 0 },
                { 0, 0, 1, 0 }
            };
            var dag = DagPattern<string>.InitDagWithMatrix(new[] { n1, n2, n3, n4 }, matrix);
            Assert.True(dag.IsDag());
        }

        [Fact]
        public void TestReachability()
        {
            const string n1 = "N1", n2 = "N2", n3 = "N3", n4 = "N4";
            var matrix = new[,]
               {
                { 0, 1, 1, 1 },
                { 1, 0, 1, 0 },
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 }
            };
            var dag = DagPattern<string>.InitDagWithMatrix(new[] { n1, n2, n3, n4 }, matrix);

            Assert.True(dag.IsReachableWithDirectedEdges(n1, n3));
            Assert.False(dag.IsReachableWithDirectedEdges(n2, n1));
        }
    }
}