// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace SafetyLustre.Oc5Compiler.ModelChecking
{
    using System.IO;
    using System.Text;
    using ISSE.SafetyChecking.Utilities;
    using System;
    using System.Linq;
    using System.Reflection;
    using ISSE.SafetyChecking.ExecutableModel;
    using ISSE.SafetyChecking.Modeling;
    using ISSE.SafetyChecking.Formula;

    public static unsafe class LustreModelSerializer
    {
        /*
        public static void WriteFault(BinaryWriter writer, Fault fault)
        {
            writer.Write(fault.Name);
            if (fault is TransientFault)
            {
                writer.Write(1);
            }
            else if (fault is PermanentFault)
            {
                writer.Write(2);
            }
            else
            {
                throw new Exception("Not implemented yet");
            }
            writer.Write(fault.Identifier);
            if (fault.ProbabilityOfOccurrence.HasValue)
                writer.Write(fault.ProbabilityOfOccurrence.Value.Value);
            else
                writer.Write(-1.0);
        }

        public static Fault ReadFault(BinaryReader reader)
        {
            var name = reader.ReadString();
            var type = reader.ReadInt32();
            var identifier = reader.ReadInt32();
            var probabilityValue = reader.ReadDouble();
            Fault fault;
            if (type == 1)
            {
                fault = new TransientFault();
            }
            else if (type == 2)
            {
                fault = new PermanentFault();
            }
            else
            {
                throw new Exception("Not implemented yet");
            }
            fault.Name = name;
            fault.Identifier = identifier;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            fault.ProbabilityOfOccurrence =
                probabilityValue == -1.0 ? null : (Probability?)new Probability(probabilityValue);
            return fault;
        }


        public static void WriteFormula(BinaryWriter writer, Formula formula, Dictionary<Fault, int> faultToIndex)
        {
            if (formula is LustrePressureBelowThreshold)
            {
                var innerFormula = (LustrePressureBelowThreshold)formula;
                writer.Write(1);
                writer.Write(innerFormula.Label);
            }
            else if (formula is FaultFormula)
            {
                var innerFormula = (FaultFormula)formula;
                writer.Write(4);
                writer.Write(innerFormula.Label);
                writer.Write(faultToIndex[innerFormula.Fault]);
            }
            else if (formula is UnaryFormula)
            {
                var innerFormula = (UnaryFormula)formula;
                writer.Write(5);
                writer.Write(innerFormula.Label);
                writer.Write((int)innerFormula.Operator);
                WriteFormula(writer, innerFormula.Operand, faultToIndex);
            }
            else if (formula is BinaryFormula)
            {
                var innerFormula = (BinaryFormula)formula;
                writer.Write(6);
                writer.Write(innerFormula.Label);
                writer.Write((int)innerFormula.Operator);
                WriteFormula(writer, innerFormula.LeftOperand, faultToIndex);
                WriteFormula(writer, innerFormula.RightOperand, faultToIndex);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static Formula ReadFormula(BinaryReader reader, Dictionary<int, Fault> indexToFault)
        {
            var type = reader.ReadInt32();
            var label = reader.ReadString();
            if (type == 1)
            {
                return new LustrePressureBelowThreshold(label);

            }
            else if (type == 4)
            {
                var index = reader.ReadInt32();
                return new FaultFormula(indexToFault[index], label);
            }
            else if (type == 5)
            {
                var @operator = (UnaryOperator)reader.ReadInt32();
                var operand = ReadFormula(reader, indexToFault);
                return new UnaryFormula(operand, @operator, label);
            }
            else if (type == 6)
            {
                var @operator = (BinaryOperator)reader.ReadInt32();
                var leftOperand = ReadFormula(reader, indexToFault);
                var rightOperand = ReadFormula(reader, indexToFault);
                return new BinaryFormula(leftOperand, @operator, rightOperand, label);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static byte[] CreateByteArray(string ocFileName, Fault[] faults, Formula[] formulas)
        {
            Requires.NotNull(ocFileName, nameof(ocFileName));

            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
            {
                // write ocFileName
                writer.Write(ocFileName);

                // write faults
                writer.Write((uint)faults.Length);
                var faultToIndex = new Dictionary<Fault, int>();
                var faultIndex = 0;
                foreach (var fault in faults)
                {
                    WriteFault(writer, fault);
                    faultToIndex.Add(fault, faultIndex);
                    faultIndex++;
                }

                // write formulas
                writer.Write((uint)formulas.Length);

                foreach (var formula in formulas)
                {
                    WriteFormula(writer, formula, faultToIndex);
                }

                // return result as array
                return buffer.ToArray();
            }
        }

        public static Tuple<LustreModelBase, Formula[]> DeserializeFromByteArray(byte[] serializedModel)
        {
            Requires.NotNull(serializedModel, nameof(serializedModel));

            using (var buffer = new MemoryStream(serializedModel))
            using (var reader = new BinaryReader(buffer, Encoding.UTF8, leaveOpen: true))
            {
                // read ocFileName
                var ocFileName = reader.ReadString();

                // read faults
                var faultNumber = reader.ReadUInt32();
                var faults = new List<Fault>();
                var indexToFault = new Dictionary<int, Fault>();
                for (var i = 0; i < faultNumber; ++i)
                {
                    var fault = ReadFault(reader);
                    faults.Add(fault);
                    indexToFault.Add(i, fault);
                }

                // read state and instantiate model
                var deserializedModel = new LustreModelBase(ocFileName, faults);

                // read formulas
                var formulaNumber = reader.ReadUInt32();
                var formulas = new Formula[formulaNumber];
                for (var i = 0; i < formulaNumber; ++i)
                {
                    formulas[i] = ReadFormula(reader, indexToFault);
                }

                //return tuple of model and formulas
                return new Tuple<LustreModelBase, Formula[]>(deserializedModel, formulas);
            }
        }

        public static SerializationDelegate CreateFastInPlaceDeserializer(LustreModelBase model)
        {
            var permanentFaults = model.faults.Values.OrderBy(fault => fault.Identifier).OfType<PermanentFault>().ToArray();

            var isActiveField = typeof(PermanentFault).GetField("_isActive", BindingFlags.NonPublic | BindingFlags.Instance);

            SerializationDelegate deserialize = state =>
            {

                var positionInRamOfFirstInt = (int*)state;
                model.program.state = positionInRamOfFirstInt[0];
                int index = 1;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 1)
                    {
                        model.program.variables[i].setValue(positionInRamOfFirstInt[index]);
                        index++;
                    }
                }

                var positionInRamOfFirstString = (char*)(positionInRamOfFirstInt + (model.program.countVariables(1) + 1));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 2)
                    {
                        model.program.variables[i].setValue(positionInRamOfFirstString[index]);
                        index++;
                    }
                }

                var positionInRamOfFirstFloat = (float*)(positionInRamOfFirstString + model.program.countVariables(2));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 3)
                    {
                        model.program.variables[i].setValue(positionInRamOfFirstFloat[index]);
                        index++;
                    }
                }

                var positionInRamOfFirstDouble = (double*)(positionInRamOfFirstFloat + model.program.countVariables(3));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 4)
                    {
                        model.program.variables[i].setValue(positionInRamOfFirstDouble[index]);
                        index++;
                    }
                }

                var positionInRamOfFirstBool = (bool*)(positionInRamOfFirstDouble + model.program.countVariables(4));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 0)
                    {
                        model.program.variables[i].setValue(positionInRamOfFirstBool[index]);
                        index++;
                    }
                }

                // Faults
                var positionInRamOfFaults = (long*)(positionInRamOfFirstBool + model.program.countVariables(0));
                var faultsSerialized = *positionInRamOfFaults;
                for (var i = 0; i < permanentFaults.Length; ++i)
                {
                    var fault = permanentFaults[i];
                    if ((faultsSerialized & (1L << i)) != 0)
                    {
                        isActiveField.SetValue(fault, true);
                    }
                    else
                    {
                        isActiveField.SetValue(fault, false);
                    }
                }

                var lastPosition = (byte*)(positionInRamOfFaults + 1);
                var length = (lastPosition - state);
                Requires.That(model.StateVectorSize == length, "model.StateVectorSize does not match");
            };
            return deserialize;
        }

        public static SerializationDelegate CreateFastInPlaceSerializer(LustreModelBase model)
        {
            var permanentFaults = model.faults.Values.OrderBy(fault => fault.Identifier).OfType<PermanentFault>().ToArray();

            var isActiveField = typeof(PermanentFault).GetField("_isActive", BindingFlags.NonPublic | BindingFlags.Instance);

            SerializationDelegate serialize = state =>
            {

                var positionInRamOfFirstInt = (int*)state;
                positionInRamOfFirstInt[0] = model.program.state;
                int index = 1;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 1)
                    {
                        positionInRamOfFirstInt[index] = (int)model.program.variables[i].getValue() > LustreQualitativeChecker.maxValue ? LustreQualitativeChecker.maxValue : (int)model.program.variables[i].getValue();
                        index++;
                    }
                }

                var positionInRamOfFirstString = (char*)(positionInRamOfFirstInt + (model.program.countVariables(1) + 1));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 2)
                    {
                        positionInRamOfFirstString[index] = (char)model.program.variables[i].getValue();
                        index++;
                    }
                }

                var positionInRamOfFirstFloat = (float*)(positionInRamOfFirstString + model.program.countVariables(2));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 3)
                    {
                        positionInRamOfFirstFloat[index] = (float)model.program.variables[i].getValue() > LustreQualitativeChecker.maxValue ? LustreQualitativeChecker.maxValue : (float)model.program.variables[i].getValue();
                        index++;
                    }
                }

                var positionInRamOfFirstDouble = (double*)(positionInRamOfFirstFloat + model.program.countVariables(3));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 4)
                    {
                        positionInRamOfFirstDouble[index] = (double)model.program.variables[i].getValue() > LustreQualitativeChecker.maxValue ? LustreQualitativeChecker.maxValue : (double)model.program.variables[i].getValue();
                        index++;
                    }
                }

                var positionInRamOfFirstBool = (bool*)(positionInRamOfFirstDouble + model.program.countVariables(4));
                index = 0;
                for (var i = 0; i < model.program.variables.Count; i++)
                {
                    if (model.program.variables[i].getType() == 0)
                    {
                        positionInRamOfFirstBool[index] = (bool)model.program.variables[i].getValue();
                        index++;
                    }
                }

                // Faults
                var positionInRamOfFaults = (long*)(positionInRamOfFirstBool + model.program.countVariables(0));
                var faultsSerialized = 0L;
                for (var i = 0; i < permanentFaults.Length; ++i)
                {
                    var fault = permanentFaults[i];
                    var isActive = (bool)isActiveField.GetValue(fault);
                    if (isActive)
                    {
                        faultsSerialized |= 1L << i;
                    }
                }
                *(positionInRamOfFaults) = faultsSerialized;
            };
            return serialize;
        }
    */
    }
}