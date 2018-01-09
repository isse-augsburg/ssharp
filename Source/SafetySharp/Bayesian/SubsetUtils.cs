// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// Copyright (c) 2017, Stefan Fritsch
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.Bayesian
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Utility methods for using subsets
    /// </summary>
    public static class SubsetUtils
    {
        /// <summary>
        /// Get the index in the subsets array for the given variables
        /// </summary>
        public static int GetIndex<T>(ICollection<T> foundVariables, IList<T> allVariables)
        {
            var sum = 0;
            for (var i = 0; i < allVariables.Count; i++)
            {
                if (foundVariables.Contains(allVariables[i]))
                {
                    sum = sum + (1 << i);
                }
            }
            return sum;
        }

        /// <summary>
        /// Get the list of variables that are present in the subset with the given index
        /// </summary>
        public static IList<T> FromIndex<T>(IList<T> variables, int index)
        {
            var foundVars = new List<T>();
            var j = 0;
            var indexAsBits = Convert.ToString(index, 2);
            for (var i = indexAsBits.Length - 1; i >= 0; i--)
            {
                if (indexAsBits[i] == '1')
                {
                    foundVars.Add(variables[j]);
                }
                j++;
            }
            return foundVars;
        }

        /// <summary>
        /// Gets all subsets of an arbitrary set with given size.
        /// For example: for elements = {1,..,n} and size 2 it will return all pairs {1,2}, {1,3}, ..., {2,3}, ... {n-1, n}
        /// </summary>
        public static IEnumerable<HashSet<T>> AllSubsets<T>(IList<T> elements, int size)
        {
            var allSubsets = new List<HashSet<T>>();
            for (var i = 1; i < (1 << elements.Count); i++)
            {
                var currentVars = new HashSet<T>();
                for (var j = 0; j < elements.Count; j++)
                {
                    if ((i & (1 << j)) > 0)
                    {
                        currentVars.Add(elements[j]);
                    }
                }
                if (currentVars.Count == size)
                {
                    allSubsets.Add(currentVars);
                }
            }
            return allSubsets;
        }
    }
}