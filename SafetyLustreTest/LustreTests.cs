using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.Utilities;
using Tests.SimpleExecutableModel;
using Xunit;
using ISSE.SafetyChecking.Modeling;
using SafetyLustre;
using Tests.Utilities;
using Xunit.Abstractions;

namespace Test {


    public class LustreModelCheckingTests {

		protected TestTraceOutput Output { get; }

		public LustreModelCheckingTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
			Program.outputTextWriter = Output.TextWriterAdapter();
		}

		[Fact]
        public void CheckPressureTankGenerateCounterExample(){
			Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\Examples\\";

			Formula invariant = new LustrePressureBelowThreshold();
            LustrePressureBelowThreshold.threshold = 30;
			var faults = new Fault[0];
			var modelChecker = new LustreQualitativeChecker("pressureTank", faults, invariant);
			modelChecker.Configuration.DefaultTraceOutput = Output.TextWriterAdapter();

			modelChecker.CheckInvariant(invariant, 100);
        }

        [Fact]
        public void CheckPressureTankInfiniteModelChecking()
		{
			Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\Examples\\";

			Formula invariant = new LustrePressureBelowThreshold();
            LustrePressureBelowThreshold.threshold = 50;
			var faults = new Fault[0];
			var modelChecker = new LustreQualitativeChecker("pressureTank", faults, invariant);
			modelChecker.Configuration.DefaultTraceOutput = Output.TextWriterAdapter();

			modelChecker.CheckInvariant(invariant, 100);
        }

        [Fact]
        public void CheckSerialization() {
            unsafe
            {
                List<Variable> variables = new List<Variable>();
                variables.Add(new Variable(0, 0, true));
                variables.Add(new Variable(1, 0, true));
                variables.Add(new Variable(2, 1, 5));
                variables.Add(new Variable(3, 0, false));
                variables.Add(new Variable(4, 1, 0));

                bool* state = (bool*)System.Runtime.InteropServices.Marshal.AllocHGlobal(11);

                var positionInRamOfFirstBool_ = (bool*)state;
                int index = 0;
                for (var i = 0; i < variables.Count; i++) {
                    if (variables[i].getType() == 0) {
                        positionInRamOfFirstBool_[index] = (bool)variables[i].getValue();
                        index++;
                    }
                }

                var positionInRamOfFirstInt_ = (int*)(state + sizeof(bool) * 4);
                index = 0;
                for (var i = 0; i < variables.Count; i++) {
                    if (variables[i].getType() == 1) {
                        positionInRamOfFirstInt_[index] = (int)variables[i].getValue();
                        index++;
                    }
                }

                variables[0].setValue(false);
                variables[1].setValue(false);
                variables[2].setValue(2);
                variables[3].setValue(true);
                variables[4].setValue(4);

                var positionInRamOfFirstBool = (bool*)state;
                index = 0;
                for (var i = 0; i < variables.Count; i++) {
                    if (variables[i].getType() == 0) {
                        variables[i].setValue(positionInRamOfFirstBool[index]);
                        index++;
                    }
                }

                var positionInRamOfFirstInt = (int*)(state + sizeof(bool) * 4);
                index = 0;
                for (var i = 0; i < variables.Count; i++) {
                    if (variables[i].getType() == 1) {
                        variables[i].setValue(positionInRamOfFirstInt[index]);
                        index++;
                    }
                }

                Requires.That((bool)variables[0].getValue() == true, "1");
                Requires.That((bool)variables[1].getValue() == true, "2");
                Requires.That((int)variables[2].getValue() == 5, "3");
                Requires.That((bool)variables[3].getValue() == false, "4");
                Requires.That((int)variables[4].getValue() == 0, "5");
            }
        }


        [Fact]
        public void CheckSerializationDelegate() {
            unsafe
			{
				Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\Examples\\";

				var faults = new Fault[0];
				LustreModelBase model = new LustreModelBase("pressureTank", faults);

                for (int i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 0) {
                        model.program.variables[i].setValue((i % 2 == 0));
                    }
                    else if (model.program.variables[i].getType() == 1) {
                        model.program.variables[i].setValue(i);
                    }
                    else if (model.program.variables[i].getType() == 3) {
                        model.program.variables[i].setValue(i);
                    }
                    else if (model.program.variables[i].getType() == 4) {
                        model.program.variables[i].setValue(i);
                    }
                }

                byte* state = (byte*)System.Runtime.InteropServices.Marshal.AllocHGlobal(model.StateVectorSize);
                LustreModelSerializer.CreateFastInPlaceSerializer(model)(state);

                for (int i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 0) {
                        model.program.variables[i].setValue((i % 2 != 0));
                    }
                    else if (model.program.variables[i].getType() == 1) {
                        model.program.variables[i].setValue(i + 3);
                    }
                    else if (model.program.variables[i].getType() == 3) {
                        model.program.variables[i].setValue(i + 3);
                    }
                    else if (model.program.variables[i].getType() == 4) {
                        model.program.variables[i].setValue(i + 3);
                    }
                }

                LustreModelSerializer.CreateFastInPlaceDeserializer(model)(state);

                for (int i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 0) {
                        Requires.That((bool)model.program.variables[i].getValue() == (i % 2 == 0), "1");
                    }
                    else if (model.program.variables[i].getType() == 1) {
                        Requires.That((int)model.program.variables[i].getValue() == i, "2");
                    }
                    else if (model.program.variables[i].getType() == 3) {
                        Requires.That((float)model.program.variables[i].getValue() == i, "3");
                    }
                    else if (model.program.variables[i].getType() == 4) {
                        Requires.That((double)model.program.variables[i].getValue() == i, "4");
                    }
                }
            }
        }
    }
}
