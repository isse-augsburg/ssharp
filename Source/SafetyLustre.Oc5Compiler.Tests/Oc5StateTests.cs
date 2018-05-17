using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SafetyLustre.Oc5Compiler.Tests
{
    [TestClass]
    public class Oc5StateTests
    {
        [TestMethod]
        public void Example1Test()
        {
            //Arrange & Act
            var oc5State = new Oc5State
            {
                Bools = new List<bool> { default(bool) },
                Ints = new List<int> { default(int), default(int), default(int) },
                Strings = new List<string>(),
                Floats = new List<float>(),
                Doubles = new List<double>(),
                Mappings = new List<PositionInOc5State>
                {
                    new PositionInOc5State {Type=PredefinedObjects.Types._boolean, IndexInOc5StateList=0 },
                    new PositionInOc5State {Type=PredefinedObjects.Types._integer, IndexInOc5StateList=0 },
                    new PositionInOc5State {Type=PredefinedObjects.Types._integer, IndexInOc5StateList=1 },
                    new PositionInOc5State {Type=PredefinedObjects.Types._integer, IndexInOc5StateList=2 },
                }
            };

            //Assert
            Assert.AreEqual(oc5State.Bools.Count, 1);
            Assert.AreEqual(oc5State.Ints.Count, 3);
        }
    }
}
