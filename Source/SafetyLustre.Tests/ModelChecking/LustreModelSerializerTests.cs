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