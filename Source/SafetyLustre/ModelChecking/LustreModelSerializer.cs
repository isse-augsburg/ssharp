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

using ISSE.SafetyChecking.ExecutableModel;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.Modeling;
using ISSE.SafetyChecking.Utilities;
using SafetyLustre.LustreCompiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static SafetyLustre.LustreSafetyAnalysis;

namespace SafetyLustre
{
    public static unsafe class LustreModelSerializer
    {
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
                throw new NotImplementedException();
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
            else if (formula is FaultFormula faultFormula)
            {
                writer.Write(4);
                writer.Write(faultFormula.Label);
                writer.Write(faultToIndex[faultFormula.Fault]);
            }
            else if (formula is UnaryFormula unaryFormula)
            {
                writer.Write(5);
                writer.Write(unaryFormula.Label);
                writer.Write((int)unaryFormula.Operator);
                WriteFormula(writer, unaryFormula.Operand, faultToIndex);
            }
            else if (formula is BinaryFormula binaryFormula)
            {
                writer.Write(6);
                writer.Write(binaryFormula.Label);
                writer.Write((int)binaryFormula.Operator);
                WriteFormula(writer, binaryFormula.LeftOperand, faultToIndex);
                WriteFormula(writer, binaryFormula.RightOperand, faultToIndex);
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

        public static byte[] CreateByteArray(string ocFileName, string mainNode, Fault[] faults, Formula[] formulas)
        {
            Requires.NotNull(ocFileName, nameof(ocFileName));
            Requires.NotNull(mainNode, nameof(mainNode));

            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
            {
                // write ocFileName
                writer.Write(ocFileName);

                //write mainNode
                writer.Write(mainNode);

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

                // read mainNode
                var mainNode = reader.ReadString();

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
                var deserializedModel = new LustreModelBase(ocFileName, mainNode, faults);

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
            var permanentFaults = model.Faults.Values.OrderBy(fault => fault.Identifier).OfType<PermanentFault>().ToArray();

            var isActiveField = typeof(PermanentFault).GetField("_isActive", BindingFlags.NonPublic | BindingFlags.Instance);

            return state =>
            {

                // Bools
                var boolPtr = (bool*)state;
                for (int i = 0; i < model.Runner.Oc5ModelState.Bools.Count; i++)
                    model.Runner.Oc5ModelState.Bools[i] = *(boolPtr++);


                // Ints
                var intPtr = (int*)boolPtr;
                for (int i = 0; i < model.Runner.Oc5ModelState.Ints.Count; i++)
                    model.Runner.Oc5ModelState.Ints[i] = *(intPtr++);

                // Strings
                var charPtr = (char*)intPtr;
                for (int i = 0; i < model.Runner.Oc5ModelState.Strings.Count; i++)
                {
                    string str = string.Empty;

                    while (*charPtr != '\0')
                        str += *(charPtr++);

                    charPtr++; // skip '\0'

                    model.Runner.Oc5ModelState.Strings[i] = str;
                }

                // Floats
                var floatPtr = (float*)charPtr;
                for (int i = 0; i < model.Runner.Oc5ModelState.Floats.Count; i++)
                    model.Runner.Oc5ModelState.Floats[i] = *(floatPtr++);

                // Double
                var doublePtr = (double*)floatPtr;
                for (int i = 0; i < model.Runner.Oc5ModelState.Doubles.Count; i++)
                    model.Runner.Oc5ModelState.Doubles[i] = *(doublePtr++);

                // Mappings
                var mappingsPtr = (PositionInOc5State*)doublePtr;
                for (int i = 0; i < model.Runner.Oc5ModelState.Mappings.Count; i++)
                    model.Runner.Oc5ModelState.Mappings[i] = *(mappingsPtr++);

                // InputMappings 
                for (int i = 0; i < model.Runner.Oc5ModelState.InputMappings.Count; i++)
                    model.Runner.Oc5ModelState.InputMappings[i] = *(mappingsPtr++);

                // OutputMappings
                for (int i = 0; i < model.Runner.Oc5ModelState.OutputMappings.Count; i++)
                    model.Runner.Oc5ModelState.OutputMappings[i] = *(mappingsPtr++);

                // CurrentState
                var currentStatePtr = (int*)mappingsPtr;
                model.Runner.Oc5ModelState.CurrentState = *(currentStatePtr++);

                // Faults
                var faultPtr = (long*)currentStatePtr;
                var faultsSerialized = *faultPtr;
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

                var lastPosition = (byte*)(faultPtr + 1);
                var length = lastPosition - state;
                Requires.That(model.StateVectorSize == length, "model.StateVectorSize does not match");
            };
        }

        public static SerializationDelegate CreateFastInPlaceSerializer(LustreModelBase model)
        {
            var permanentFaults = model.Faults.Values.OrderBy(fault => fault.Identifier).OfType<PermanentFault>().ToArray();

            var isActiveField = typeof(PermanentFault).GetField("_isActive", BindingFlags.NonPublic | BindingFlags.Instance);

            return state =>
            {
                // Bools
                var boolPtr = (bool*)state;
                foreach (var b in model.Runner.Oc5ModelState.Bools)
                    *(boolPtr++) = b;


                // Ints
                var intPtr = (int*)boolPtr;
                foreach (var i in model.Runner.Oc5ModelState.Ints)
                    *(intPtr++) = i > LustreQualitativeChecker.maxValue ? LustreQualitativeChecker.maxValue : i;

                // Strings
                var charPtr = (char*)intPtr;
                foreach (var s in model.Runner.Oc5ModelState.Strings)
                {
                    var length = Math.Max(29, s.Length);
                    foreach (var c in s.ToCharArray(0, length))
                    {
                        *(charPtr++) = c;
                    }
                    charPtr += 30 - length;
                    *(charPtr++) = '\0';
                }

                // Floats
                var floatPtr = (float*)charPtr;
                foreach (var f in model.Runner.Oc5ModelState.Floats)
                    *(floatPtr++) = f > LustreQualitativeChecker.maxValue ? LustreQualitativeChecker.maxValue : f;

                // Double
                var doublePtr = (double*)floatPtr;
                foreach (var d in model.Runner.Oc5ModelState.Doubles)
                    *(doublePtr++) = d > LustreQualitativeChecker.maxValue ? LustreQualitativeChecker.maxValue : d;

                // Mappings
                var mappingsPtr = (PositionInOc5State*)doublePtr;
                foreach (var m in model.Runner.Oc5ModelState.Mappings)
                    *(mappingsPtr++) = m;

                // InputMappings 
                foreach (var im in model.Runner.Oc5ModelState.InputMappings)
                    *(mappingsPtr++) = im;

                // OutputMappings
                foreach (var om in model.Runner.Oc5ModelState.OutputMappings)
                    *(mappingsPtr++) = om;

                // CurrentState
                var currentStatePtr = (int*)mappingsPtr;
                *(currentStatePtr++) = model.Runner.Oc5ModelState.CurrentState;

                // Faults
                var faultPtr = (long*)currentStatePtr;
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
                *(faultPtr) = faultsSerialized;
            };
        }
    }
}