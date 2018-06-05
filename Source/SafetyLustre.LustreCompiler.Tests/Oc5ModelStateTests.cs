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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace SafetyLustre.LustreCompiler.Tests
{
    [TestClass]
    public class Oc5ModelStateTests
    {
        [TestMethod]
        public void TestExample1()
        {
            //Arrange
            var oc5Source = File.ReadAllText("Examples/example1.oc");

            //Act
            var runner = new Oc5Runner(oc5Source);
            var oc5ModelState = runner.Oc5ModelState;

            //Assert
            Assert.AreEqual(oc5ModelState.Bools.Count, 1);
            Assert.AreEqual(oc5ModelState.Ints.Count, 3);
        }

        [TestMethod]
        public void TestExample2()
        {
            //Arrange
            var oc5Source = File.ReadAllText("Examples/example2.oc");

            //Act
            var runner = new Oc5Runner(oc5Source);
            var oc5ModelState = runner.Oc5ModelState;

            //Assert
            Assert.AreEqual(oc5ModelState.Bools.Count, 3);
        }

        [TestMethod]
        public void TestExample3()
        {
            //Arrange
            var oc5Source = File.ReadAllText("Examples/example3.oc");

            //Act
            var runner = new Oc5Runner(oc5Source);
            var oc5ModelState = runner.Oc5ModelState;

            //Assert
            Assert.AreEqual(oc5ModelState.Bools.Count, 3);
        }

        [TestMethod]
        public void TestExample4()
        {
            //Arrange
            var oc5Source = File.ReadAllText("Examples/example4.oc");

            //Act
            var runner = new Oc5Runner(oc5Source);
            var oc5ModelState = runner.Oc5ModelState;

            //Assert
            Assert.AreEqual(oc5ModelState.Bools.Count, 5);
        }

        [TestMethod]
        public void TestExample5()
        {
            //Arrange
            var oc5Source = File.ReadAllText("Examples/example5.oc");

            //Act
            var runner = new Oc5Runner(oc5Source);
            var oc5ModelState = runner.Oc5ModelState;

            //Assert
            Assert.AreEqual(oc5ModelState.Bools.Count, 5);
        }

        [TestMethod]
        public void TestExample6()
        {
            //Arrange
            var oc5Source = File.ReadAllText("Examples/example6.oc");

            //Act
            var runner = new Oc5Runner(oc5Source);
            var oc5ModelState = runner.Oc5ModelState;

            //Assert
            Assert.AreEqual(oc5ModelState.Bools.Count, 3);
            Assert.AreEqual(oc5ModelState.Ints.Count, 3);
        }

        [TestMethod]
        public void TestPressureTank()
        {
            //Arrange
            var oc5Source = File.ReadAllText("Examples/pressureTank.oc");

            //Act
            var runner = new Oc5Runner(oc5Source);
            var oc5ModelState = runner.Oc5ModelState;

            //Assert
            Assert.AreEqual(oc5ModelState.Bools.Count, 5);
            Assert.AreEqual(oc5ModelState.Ints.Count, 2);
        }
    }
}
