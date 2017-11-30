namespace Tests.Bayesian
{
    using System.Collections.Generic;
    using System.Linq;
    using SafetySharp.Bayesian;
    using Xunit;

    public class SubsetUtilsTests
    {

        [Fact]
        public void TestGetIndex()
        {
            var allVariables = new[] { "A", "B", "C", "D" };
            var foundVariables = new[] { "D", "B" };
            var index = SubsetUtils.GetIndex(foundVariables, allVariables);

            Assert.Equal(10, index);
        }

        [Fact]
        public void TestFromIndex()
        {
            var allVariables = new[] { "A", "B", "C", "D" };
            const int index = 11;
            var variablesFromIndex = SubsetUtils.FromIndex(allVariables, index);
            var expected = new[] { "A", "B", "D" };

            Assert.True(!expected.Except(variablesFromIndex).Any() && expected.Length == variablesFromIndex.Count);
        }

        [Fact]
        public void TestFromAndGetIndex()
        {
            const int index = 103;
            var allVariables = new[] { "A", "B", "C", "D", "E", "F", "G" };
            var variablesFromIndex = SubsetUtils.FromIndex(allVariables, index);
            var calculatedIndex = SubsetUtils.GetIndex(variablesFromIndex, allVariables);

            Assert.Equal(index, calculatedIndex);
        }

        [Fact]
        public void TestAllSubsetsSize0()
        {
            var allVariables = new[] { "A", "B", "C" };
            var subsets = SubsetUtils.AllSubsets<string>(allVariables, 0);

            Assert.Equal(0, subsets.Count());
        }

        [Fact]
        public void TestAllSubsetsSize1()
        {
            var allVariables = new[] { "A", "B", "C" };
            var subsets = SubsetUtils.AllSubsets<string>(allVariables, 1);
            var expected = new[]
            {
                new HashSet<string>() { "A" },
                new HashSet<string>() { "B" },
                new HashSet<string>() { "C" }
            };

            Assert.True(ContainsSameSubsets(subsets, expected));
            Assert.True(ContainsSameSubsets(expected, subsets));
        }

        [Fact]
        public void TestAllSubsetsSize2()
        {
            var allVariables = new[] { "A", "B", "C" };
            var subsets = SubsetUtils.AllSubsets<string>(allVariables, 2);
            var expected = new[]
            {
                new HashSet<string>() { "A", "B" },
                new HashSet<string>() { "A", "C" },
                new HashSet<string>() { "B", "C" }
            };

            Assert.True(ContainsSameSubsets(subsets, expected));
            Assert.True(ContainsSameSubsets(expected, subsets));
        }

        [Fact]
        public void TestAllSubsetsSize3()
        {
            var allVariables = new[] { "A", "B", "C" };
            var subsets = SubsetUtils.AllSubsets<string>(allVariables, 3);
            var expected = new[]
            {
                new HashSet<string>() { "A", "B", "C" }
            };

            Assert.True(ContainsSameSubsets(subsets, expected));
            Assert.True(ContainsSameSubsets(expected, subsets));
        }

        private static bool ContainsSameSubsets(IEnumerable<HashSet<string>> elements, IEnumerable<HashSet<string>> container)
        {
            foreach (var element in elements)
            {
                var isContained = false;
                foreach (var containerElement in container)
                {
                    if (!element.Except(containerElement).Any() && element.Count == containerElement.Count)
                    {
                        isContained = true;
                    }
                }
                if (!isContained)
                {
                    return false;
                }
            }
            return true;
        }
    }
}