// The MIT License (MIT)
// 
// Copyright (c) 2014-2018, Institute for Software & Systems Engineering
// Copyright (c) 2018, Pascal Pfeil
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

using SafetyLustre.LustreCompiler.Oc5Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SafetyLustre.LustreCompiler
{
    class Oc5ModelState
    {
        public List<bool> Bools { get; set; } = new List<bool>();
        public List<int> Ints { get; set; } = new List<int>();
        public List<string> Strings { get; set; } = new List<string>();
        public List<float> Floats { get; set; } = new List<float>();
        public List<double> Doubles { get; set; } = new List<double>();
        /// <summary>
        /// This List maps the variable index in the oc5 file (e.g. 0:)
        /// to a type and the index in the <see cref="Bools"/>, <see cref="Ints"/>,
        /// <see cref="Strings"/>, <see cref="Floats"/> or <see cref="Doubles"/> list.
        /// </summary>
        public List<PositionInOc5State> Mappings { get; set; } = new List<PositionInOc5State>();
        /// <summary>
        /// This List maps input index to a type and the index in the <see cref="Bools"/>,
        /// <see cref="Ints"/>, <see cref="Strings"/>, <see cref="Floats"/>
        /// or <see cref="Doubles"/> list.
        /// </summary>
        internal List<PositionInOc5State> InputMappings { get; set; }
        /// <summary>
        /// This List maps input index to a type and the index in the <see cref="Bools"/>,
        /// <see cref="Ints"/>, <see cref="Strings"/>, <see cref="Floats"/>
        /// or <see cref="Doubles"/> list.
        /// </summary>
        internal List<PositionInOc5State> OutputMappings { get; set; }
        /// <summary>
        /// Represents the current oc5 state the model is in.
        /// </summary>
        public int CurrentState { get; set; }

        public void SetupInputOutputMappings(IEnumerable<Signal> signals)
        {
            InputMappings = signals.OfType<SingleInputSignal>()
                .Select(signal => Mappings[signal.VarIndex])
                .ToList();

            OutputMappings = signals.OfType<SingleOutputSignal>()
                .Select(signal => Mappings[signal.VarIndex])
                .ToList();
        }

        public void AssignInputs(params object[] inputs)
        {
            InputMappings
                .ToList()
                .ForEach(
                    (mapping) =>
                    {
                        var indexInInputs = InputMappings.IndexOf(mapping);
                        var indexInOc5StateList = mapping.IndexInOc5StateList;

                        switch (mapping.Type)
                        {
                            case PredefinedObjects.Types._boolean:
                                Bools[indexInOc5StateList] = (bool)inputs[indexInInputs];
                                break;
                            case PredefinedObjects.Types._integer:
                                Ints[indexInOc5StateList] = (int)inputs[indexInInputs];
                                break;
                            case PredefinedObjects.Types._string:
                                Strings[indexInOc5StateList] = (string)inputs[indexInInputs];
                                break;
                            case PredefinedObjects.Types._float:
                                Floats[indexInOc5StateList] = (float)inputs[indexInInputs];
                                break;
                            case PredefinedObjects.Types._double:
                                Doubles[indexInOc5StateList] = (double)inputs[indexInInputs];
                                break;
                            default:
                                throw new ArgumentException($"No predefined type with index {mapping.Type}!");
                        }
                    }
                );
        }

        public IEnumerable<object> GetOutputs()
        {
            return OutputMappings
                .Select<PositionInOc5State, object>(mapping =>
                    {
                        var indexInInputs = InputMappings.IndexOf(mapping);
                        var indexInOc5StateList = mapping.IndexInOc5StateList;

                        switch (mapping.Type)
                        {
                            case PredefinedObjects.Types._boolean:
                                return Bools[indexInOc5StateList];
                            case PredefinedObjects.Types._integer:
                                return Ints[indexInOc5StateList];
                            case PredefinedObjects.Types._string:
                                return Strings[indexInOc5StateList];
                            case PredefinedObjects.Types._float:
                                return Floats[indexInOc5StateList];
                            case PredefinedObjects.Types._double:
                                return Doubles[indexInOc5StateList];
                            default:
                                throw new ArgumentException($"No predefined type with index {mapping.Type}!");
                        }
                    }
                );
        }
    }

    [StructLayout(LayoutKind.Auto)]
    struct PositionInOc5State
    {
        public PredefinedObjects.Types Type { get; set; }
        public int IndexInOc5StateList { get; set; }
    }
}
