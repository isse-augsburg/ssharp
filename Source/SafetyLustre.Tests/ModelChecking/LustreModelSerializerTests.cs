using ISSE.SafetyChecking.Modeling;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            Console.WriteLine($"Allocated {model.StateVectorSize} bytes of memory.");

            var bools = model.Runner.Oc5ModelState.Bools;
            var ints = model.Runner.Oc5ModelState.Ints;
            var strings = model.Runner.Oc5ModelState.Strings;
            var floats = model.Runner.Oc5ModelState.Floats;
            var doubles = model.Runner.Oc5ModelState.Doubles;
            var mappings = model.Runner.Oc5ModelState.Mappings;
            var inputMappings = model.Runner.Oc5ModelState.InputMappings;
            var outputMappings = model.Runner.Oc5ModelState.OutputMappings;

            //Act
            var deserializer = LustreModelSerializer.CreateFastInPlaceDeserializer(model);
            var serializer = LustreModelSerializer.CreateFastInPlaceSerializer(model);

            deserializer(memory);
            serializer(memory);

            //Assert
            Assert.IsTrue(model.Runner.Oc5ModelState.Bools.SequenceEqual(bools));
            Assert.IsTrue(model.Runner.Oc5ModelState.Ints.SequenceEqual(ints));
            Assert.IsTrue(model.Runner.Oc5ModelState.Strings.SequenceEqual(strings));
            Assert.IsTrue(model.Runner.Oc5ModelState.Floats.SequenceEqual(floats));
            Assert.IsTrue(model.Runner.Oc5ModelState.Doubles.SequenceEqual(doubles));
            Assert.IsTrue(model.Runner.Oc5ModelState.Mappings.SequenceEqual(mappings));
            Assert.IsTrue(model.Runner.Oc5ModelState.InputMappings.SequenceEqual(inputMappings));
            Assert.IsTrue(model.Runner.Oc5ModelState.OutputMappings.SequenceEqual(outputMappings));
        }
    }
}