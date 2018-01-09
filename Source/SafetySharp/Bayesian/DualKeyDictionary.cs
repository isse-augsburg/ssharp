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
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// A dictionary that maps two keys to a value without regarding the order of the keys.
    /// </summary>
    internal class DualKeyDictionary<TKey, TValue> : IEnumerable<Tuple<TKey, TKey>>
    {
        private readonly Dictionary<Tuple<TKey, TKey>, TValue> _dictionary = new Dictionary<Tuple<TKey, TKey>, TValue>();

        public TValue this[TKey first, TKey second]
        {
            get
            {
                var tuple = Tuple(first, second);
                var reverseTuple = Tuple(second, first);
                if (_dictionary.ContainsKey(reverseTuple))
                {
                    return _dictionary[reverseTuple];
                }
                return _dictionary[tuple];
            }
            set
            {
                var tuple = Tuple(first, second);
                var tupleReverse = Tuple(second, first);
                if (_dictionary.ContainsKey(tuple))
                {
                    _dictionary.Remove(tuple);
                }
                if (_dictionary.ContainsKey(tupleReverse))
                {
                    _dictionary.Remove(tupleReverse);
                }
                _dictionary.Add(tuple, value);
            }
        }

        public TValue this[Tuple<TKey, TKey> tuple]
        {
            get
            {
                var reverseTuple = Tuple(tuple.Item2, tuple.Item1);
                if (_dictionary.ContainsKey(reverseTuple))
                {
                    return _dictionary[reverseTuple];
                }
                return _dictionary[tuple];
            }
            set
            {
                var reverseTuple = Tuple(tuple.Item2, tuple.Item1);
                if (_dictionary.ContainsKey(tuple))
                {
                    _dictionary.Remove(tuple);
                }
                if (_dictionary.ContainsKey(reverseTuple))
                {
                    _dictionary.Remove(reverseTuple);
                }
                _dictionary.Add(tuple, value);
            }
        }

        public bool ContainsKey(TKey first, TKey second)
        {
            var tuple = Tuple(first, second);
            var reverseTuple = Tuple(second, first);
            return _dictionary.ContainsKey(tuple) || _dictionary.ContainsKey(reverseTuple);
        }


        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Tuple<TKey, TKey>> GetEnumerator()
        {
            return _dictionary.Keys.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("{");
            builder.AppendLine();
            foreach (var tuple in this)
            {
                builder.Append($"[{tuple.Item1},{tuple.Item2}] -> {this[tuple]}");
                builder.AppendLine();
            }
            builder.Append("}");
            return builder.ToString();
        }

        private static Tuple<TKey, TKey> Tuple(TKey first, TKey second)
        {
            return new Tuple<TKey, TKey>(first, second);
        }
    }
}
