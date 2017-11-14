namespace Tests.Bayesian
{
    using System.Collections.Generic;
    using SafetySharp.Bayesian;
    using Xunit;

    public class ConstraintBasedStructureLearnerTests
    {

        [Fact]
        public void TestModel2()
        {
            var fl = new BooleanRandomVariable(() => true, "FL");
            var fv = new BooleanRandomVariable(() => true, "FV");
            var h = new BooleanRandomVariable(() => true, "H");
            var independencies = new DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>>
            {
                [h, fl] = new[] { new HashSet<RandomVariable> { fv } }
            };
            var learning = new ConstraintBasedStructureLearner(new[] { fl, fv, h }, independencies);
            learning.LearnDag();
        }

        [Fact]
        public void TestExample()
        {
            var x = new BooleanRandomVariable(() => true, "x");
            var y = new BooleanRandomVariable(() => true, "y");
            var z = new BooleanRandomVariable(() => true, "z");
            var w = new BooleanRandomVariable(() => true, "w");
            var t = new BooleanRandomVariable(() => true, "t");
            var independencies = new DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>>
            {
                [x, y] = new[] { new HashSet<RandomVariable>() }, // x to y
                [z, w] = new[] { new HashSet<RandomVariable> { x, y } }, // z to w given x,y
                [w, x] = new[] { new HashSet<RandomVariable> { y } }, // w to x given y
                [w, z] = new[] { new HashSet<RandomVariable> { y } }, // w to z given y
                [t, x] = new[] { new HashSet<RandomVariable> { z, w } }, // t to x given z,w
                [t, y] = new[] { new HashSet<RandomVariable> { z, w } } //t to y given z,w
            };
            var learning = new ConstraintBasedStructureLearner(new[] { x, y, z, w, t }, independencies);
            learning.LearnDag();
        }

        [Fact]
        public void Example10_1()
        {
            var x = new BooleanRandomVariable(() => true, "x");
            var y = new BooleanRandomVariable(() => true, "y");
            var independencies = new DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>>
            {
                [x, y] = new[] { new HashSet<RandomVariable>() } // X to Y
            };
            var learning = new ConstraintBasedStructureLearner(new[] { x, y }, independencies);
            var dag = learning.LearnDag();

            var expected = new[,]
            {
                { 0, 0 },
                { 0, 0 }
            };
            Assert.True(dag.EqualsAdjacencyMatrix(expected));
        }


        [Fact]
        public void Example10_2()
        {
            var x = new BooleanRandomVariable(() => true, "x");
            var y = new BooleanRandomVariable(() => true, "y");
            var independencies = new DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>>();
            var learning = new ConstraintBasedStructureLearner(new[] { x, y }, independencies);
            var dag = learning.LearnDag();

            var expected = new[,]
            {
                { 0, 1 },
                { 1, 0 }
            };
            Assert.True(dag.EqualsAdjacencyMatrix(expected));
        }


        [Fact]
        public void Example10_3()
        {
            var x = new BooleanRandomVariable(() => true, "x");
            var y = new BooleanRandomVariable(() => true, "y");
            var z = new BooleanRandomVariable(() => true, "z");
            var independencies = new DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>>
            {
                [x, y] = new[] { new HashSet<RandomVariable> { z } } // X to Y given Z
            };
            var learning = new ConstraintBasedStructureLearner(new[] { x, y, z }, independencies);
            var dag = learning.LearnDag();

            var expected = new[,]
            {
                { 0, 0, 1 },
                { 0, 0, 1 },
                { 1, 1, 0 }
            };
            Assert.True(dag.EqualsAdjacencyMatrix(expected));
        }

        [Fact]
        public void Example10_4()
        {
            var x = new BooleanRandomVariable(() => true, "x");
            var y = new BooleanRandomVariable(() => true, "y");
            var z = new BooleanRandomVariable(() => true, "z");
            var independencies = new DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>>
            {
                [x, y] = new[] { new HashSet<RandomVariable>() } // X to Y
            };
            var learning = new ConstraintBasedStructureLearner(new[] { x, y, z }, independencies);
            var dag = learning.LearnDag();

            var expected = new[,]
            {
                { 0, 0, 1 },
                { 0, 0, 1 },
                { 0, 0, 0 }
            };
            Assert.True(dag.EqualsAdjacencyMatrix(expected));
        }

        [Fact]
        public void Example10_5()
        {
            var x = new BooleanRandomVariable(() => true, "x");
            var y = new BooleanRandomVariable(() => true, "y");
            var z = new BooleanRandomVariable(() => true, "z");
            var w = new BooleanRandomVariable(() => true, "w");
            var independencies = new DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>>
            {
                [x, y] = new[] { new HashSet<RandomVariable>() }, // X to Y
                [x, w] = new[] { new HashSet<RandomVariable> { z } }, // X to W given Z
                [y, w] = new[] { new HashSet<RandomVariable> { z } } // Y to W given Z
            };
            var learning = new ConstraintBasedStructureLearner(new[] { x, y, z, w }, independencies);
            var dag = learning.LearnDag();

            var expected = new[,]
            {
                { 0, 0, 1, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 },
                { 0, 0, 0, 0 }
            };
            Assert.True(dag.EqualsAdjacencyMatrix(expected));
        }

        [Fact]
        public void Example10_6()
        {
            var x = new BooleanRandomVariable(() => true, "x");
            var y = new BooleanRandomVariable(() => true, "y");
            var z = new BooleanRandomVariable(() => true, "z");
            var u = new BooleanRandomVariable(() => true, "u");
            var w = new BooleanRandomVariable(() => true, "w");
            var independencies = new DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>>
            {
                [x, y] = new[] { new HashSet<RandomVariable> { u } }, // X to Y given U
                [u, z] = new[] { new HashSet<RandomVariable> { x, y } }, // U to Z given X,Y
                [u, w] = new[] { new HashSet<RandomVariable> { x, y }, new HashSet<RandomVariable> { z } }, // U to W given {X,Y} and given {Z}
                [x, w] = new[] { new HashSet<RandomVariable> { z } }, // X to W given Z
                [y, w] = new[] { new HashSet<RandomVariable> { z } }, // Y to W given Z
            };
            var learning = new ConstraintBasedStructureLearner(new[] { x, y, z, u, w }, independencies);
            var dag = learning.LearnDag();

            var expected = new[,]
            {
                { 0, 0, 1, 1, 0 },
                { 0, 0, 1, 1, 0 },
                { 0, 0, 0, 0, 1 },
                { 1, 1, 0, 0, 0 },
                { 0, 0, 0, 0, 0 }
            };
            Assert.True(dag.EqualsAdjacencyMatrix(expected));
        }
    }
}