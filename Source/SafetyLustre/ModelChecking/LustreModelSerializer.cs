// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace Tests.SimpleExecutableModel {
    using System.IO;
    using System.Text;
    using ISSE.SafetyChecking.Utilities;
    using System;
    using System.Linq;
    using System.Reflection;
    using ISSE.SafetyChecking.ExecutableModel;
    using ISSE.SafetyChecking.Modeling;
    using ISSE.SafetyChecking.Formula;

    public static unsafe class LustreModelSerializer {
        public static void WriteFormula(BinaryWriter writer, Formula formula) {
			if (formula is LustrePressureBelowThreshold)
			{
				var innerFormula = (LustrePressureBelowThreshold)formula;
				writer.Write(1);
				writer.Write(innerFormula.Label);
			}
			else if (formula is UnaryFormula)
			{
				var innerFormula = (UnaryFormula)formula;
				writer.Write(4);
				writer.Write(innerFormula.Label);
				writer.Write((int)innerFormula.Operator);
				WriteFormula(writer, innerFormula.Operand);
			}
			else if (formula is BinaryFormula)
			{
				var innerFormula = (BinaryFormula)formula;
				writer.Write(5);
				writer.Write(innerFormula.Label);
				writer.Write((int)innerFormula.Operator);
				WriteFormula(writer, innerFormula.LeftOperand);
				WriteFormula(writer, innerFormula.RightOperand);
			}
			else
			{
				throw new NotImplementedException();
			}
        }

        public static Formula ReadFormula(BinaryReader reader) {
			var type = reader.ReadInt32();
			var label = reader.ReadString();
			if (type==1)
			{
				return new LustrePressureBelowThreshold(label);

			}
			else if (type == 4)
			{
				var @operator = (UnaryOperator)reader.ReadInt32();
				var operand = ReadFormula(reader);
				return new UnaryFormula(operand, @operator, label);
			}
			else if (type == 5)
			{
				var @operator = (BinaryOperator)reader.ReadInt32();
				var leftOperand = ReadFormula(reader);
				var rightOperand = ReadFormula(reader);
				return new BinaryFormula(leftOperand, @operator, rightOperand, label);
			}
			else
			{
				throw new NotImplementedException();
			}
        }

        public static byte[] CreateByteArray(string ocFileName, Formula[] formulas) {
            Requires.NotNull(ocFileName, nameof(ocFileName));

            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true)) {
                // write ocFileName
                writer.Write(ocFileName);

                // write formulas
                writer.Write((uint)formulas.Length);
                foreach (var formula in formulas) {
                    WriteFormula(writer, formula);
                }

                // return result as array
                return buffer.ToArray();
            }
        }

        public static Tuple<LustreModelBase, Formula[]> DeserializeFromByteArray(byte[] serializedModel) {
            Requires.NotNull(serializedModel, nameof(serializedModel));

            using (var buffer = new MemoryStream(serializedModel))
            using (var reader = new BinaryReader(buffer, Encoding.UTF8, leaveOpen: true)) {
                // read ocFileName
                var ocFileName = reader.ReadString();

                // read state and instantiate model
                var deserializedModel = new LustreModelBase(ocFileName);

                // read formulas
                var formulaNumber = reader.ReadUInt32();
                var formulas = new Formula[formulaNumber];
                for (var i = 0; i < formulaNumber; ++i) {
                    formulas[i] = ReadFormula(reader);
                }

                //return tuple of model and formulas
                return new Tuple<LustreModelBase, Formula[]>(deserializedModel, formulas);
            }
        }

        public static SerializationDelegate CreateFastInPlaceDeserializer(LustreModelBase model) {
            SerializationDelegate deserialize = state => {

                var positionInRamOfFirstInt = (int*)state;
                model.program.state = positionInRamOfFirstInt[0];
                int index = 1;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 1) {
                        model.program.variables[i].setValue(positionInRamOfFirstInt[index]);
                        index++;
                    }
                }

                var positionInRamOfFirstString = (char*)(positionInRamOfFirstInt + (model.program.countVariables(1) + 1));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 2) {
                        model.program.variables[i].setValue(positionInRamOfFirstString[index]);
                        index++;
                    }
                }

                var positionInRamOfFirstFloat = (float*)(positionInRamOfFirstString + model.program.countVariables(2));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 3) {
                        model.program.variables[i].setValue(positionInRamOfFirstFloat[index]);
                        index++;
                    }
                }

                var positionInRamOfFirstDouble = (double*)(positionInRamOfFirstFloat + model.program.countVariables(3));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 4) {
                        model.program.variables[i].setValue(positionInRamOfFirstDouble[index]);
                        index++;
                    }
                }

                var positionInRamOfFirstBool = (bool*)(positionInRamOfFirstDouble + model.program.countVariables(4));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 0) {
                        model.program.variables[i].setValue(positionInRamOfFirstBool[index]);
                        index++;
                    }
                }
                var lastPosition = (byte*)(positionInRamOfFirstBool + model.program.countVariables(0));
                var length = (lastPosition - state);
                Requires.That(model.StateVectorSize == length, "model.StateVectorSize does not match");
            };
            return deserialize;
        }


        public static SerializationDelegate CreateFastInPlaceSerializer(LustreModelBase model) {
            SerializationDelegate serialize = state => {

                var positionInRamOfFirstInt = (int*)state;
                positionInRamOfFirstInt[0] = model.program.state;
                int index = 1;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 1) {
                        positionInRamOfFirstInt[index] = (int)model.program.variables[i].getValue() > LustreQualitativeChecker.maxValue ? LustreQualitativeChecker.maxValue : (int)model.program.variables[i].getValue();
                        index++;
                    }
                }

                var positionInRamOfFirstString = (char*)(positionInRamOfFirstInt + (model.program.countVariables(1) + 1));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 2) {
                        positionInRamOfFirstString[index] = (char)model.program.variables[i].getValue();
                        index++;
                    }
                }

                var positionInRamOfFirstFloat = (float*)(positionInRamOfFirstString + model.program.countVariables(2));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 3) {
                        positionInRamOfFirstFloat[index] = (float)model.program.variables[i].getValue() > LustreQualitativeChecker.maxValue ? LustreQualitativeChecker.maxValue : (float)model.program.variables[i].getValue();
                        index++;
                    }
                }

                var positionInRamOfFirstDouble = (double*)(positionInRamOfFirstFloat + model.program.countVariables(3));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 4) {
                        positionInRamOfFirstDouble[index] = (double)model.program.variables[i].getValue() > LustreQualitativeChecker.maxValue ? LustreQualitativeChecker.maxValue : (double)model.program.variables[i].getValue();
                        index++;
                    }
                }

                var positionInRamOfFirstBool = (bool*)(positionInRamOfFirstDouble + model.program.countVariables(4));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++) {
                    if (model.program.variables[i].getType() == 0) {
                        positionInRamOfFirstBool[index] = (bool)model.program.variables[i].getValue();
                        index++;
                    }
                }
            };
            return serialize;
        }
    }
}
