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

using ISSE.SafetyChecking.Modeling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SafetyLustre.LustreCompiler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SafetyLustre.Tests
{
    [TestClass()]
    public unsafe class LustreModelSerializerTests
    {
        [TestMethod()]
        public void TestModelSerialization()
        {
            //Arrange
            var model = new LustreModelBase(@"Examples\pressureTank.lus", "TANK", new List<Fault>());
            var memory = stackalloc byte[model.StateVectorSize];

            // Use toArray for a deep copy
            var bools = model.Runner.Oc5ModelState.Bools.ToArray();
            var ints = model.Runner.Oc5ModelState.Ints.ToArray();
            var strings = model.Runner.Oc5ModelState.Strings.ToArray();
            var floats = model.Runner.Oc5ModelState.Floats.ToArray();
            var doubles = model.Runner.Oc5ModelState.Doubles.ToArray();
            var mappings = model.Runner.Oc5ModelState.Mappings.ToArray();
            var inputMappings = model.Runner.Oc5ModelState.InputMappings.ToArray();
            var outputMappings = model.Runner.Oc5ModelState.OutputMappings.ToArray();
            var state = model.Runner.Oc5ModelState.CurrentState;

            //Act
            var serializer = LustreModelSerializer.CreateFastInPlaceSerializer(model);
            var deserializer = LustreModelSerializer.CreateFastInPlaceDeserializer(model);

            serializer(memory);
            deserializer(memory);

            //Assert
            Assert.IsTrue(model.Runner.Oc5ModelState.Bools.SequenceEqual(bools));
            Assert.IsTrue(model.Runner.Oc5ModelState.Ints.SequenceEqual(ints));
            Assert.IsTrue(model.Runner.Oc5ModelState.Strings.SequenceEqual(strings));
            Assert.IsTrue(model.Runner.Oc5ModelState.Floats.SequenceEqual(floats));
            Assert.IsTrue(model.Runner.Oc5ModelState.Doubles.SequenceEqual(doubles));
            Assert.IsTrue(model.Runner.Oc5ModelState.Mappings.SequenceEqual(mappings));
            Assert.IsTrue(model.Runner.Oc5ModelState.InputMappings.SequenceEqual(inputMappings));
            Assert.IsTrue(model.Runner.Oc5ModelState.OutputMappings.SequenceEqual(outputMappings));
            Assert.IsTrue(model.Runner.Oc5ModelState.CurrentState == state);
        }
    }
}