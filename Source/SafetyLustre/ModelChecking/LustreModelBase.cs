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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ISSE.SafetyChecking.Modeling;
using SafetyLustre.LustreCompiler;

namespace SafetyLustre
{
    public unsafe class LustreModelBase
    {
        public Dictionary<string, Fault> Faults { get; set; }

        public int StateVectorSize { get; }

        public Choice Choice { get; set; } = new Choice();

        public List<object> Outputs = new List<object>();

        public Oc5Runner Runner { get; set; }

        public LustreModelBase(string lustrePath, string mainNode, IEnumerable<Fault> faults)
        {
            var oc5 = LusCompiler.Compile(File.ReadAllText(lustrePath), mainNode);
            Runner = new Oc5Runner(oc5);

            StateVectorSize =
                Runner.Oc5ModelState.Bools.Count * sizeof(bool) +
                Runner.Oc5ModelState.Ints.Count * sizeof(int) +
                Runner.Oc5ModelState.Strings.Count * sizeof(char) * 30 +                    //HACK max. length of string is 30 chars (29 + '\0')
                Runner.Oc5ModelState.Floats.Count * sizeof(float) +
                Runner.Oc5ModelState.Doubles.Count * sizeof(double) +
                Runner.Oc5ModelState.Mappings.Count * sizeof(PositionInOc5State) +
                Runner.Oc5ModelState.InputMappings.Count * sizeof(PositionInOc5State) +
                Runner.Oc5ModelState.OutputMappings.Count * sizeof(PositionInOc5State) +
                sizeof(int) +                                                               // variable containing the current state
                sizeof(long);                                                               // variable containing the permanent faults
            Faults = faults.ToDictionary(fault => fault.Name, fault => fault);
        }

        public virtual void SetInitialState()
        {
            Outputs.Clear();
            Outputs.Add(0);
        }

        public void Update()
        {
            var value = Choice.Choose(true, false);
            Runner.Tick(value);

            Outputs = Runner.Oc5ModelState.GetOutputs().ToList();
        }
    }
}
