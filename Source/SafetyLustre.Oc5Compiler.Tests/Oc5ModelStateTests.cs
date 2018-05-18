using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace SafetyLustre.Oc5Compiler.Tests
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
