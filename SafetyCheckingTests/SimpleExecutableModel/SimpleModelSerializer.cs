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

namespace Tests.SimpleExecutableModel
{
	using System.IO;
	using System.Text;
	using ISSE.SafetyChecking.Utilities;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using ISSE.SafetyChecking.ExecutableModel;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Formula;

	public static unsafe class SimpleModelSerializer
	{
		public static void WriteFormula(BinaryWriter writer, Formula formula, Dictionary<Fault,int> faultToIndex)
		{
			if (formula is SimpleStateInRangeFormula)
			{
				var innerFormula = (SimpleStateInRangeFormula)formula;
				writer.Write(1);
				writer.Write(innerFormula.Label);
				writer.Write(innerFormula.From);
				writer.Write(innerFormula.To);
			}
			else if (formula is SimpleLocalVarInRangeFormula)
			{
				var innerFormula = (SimpleLocalVarInRangeFormula)formula;
				writer.Write(2);
				writer.Write(innerFormula.Label);
				writer.Write(innerFormula.Index);
				writer.Write(innerFormula.From);
				writer.Write(innerFormula.To);
			}
			else if (formula is SimpleLocalVarIsTrue)
			{
				var innerFormula = (SimpleLocalVarIsTrue)formula;
				writer.Write(3);
				writer.Write(innerFormula.Label);
				writer.Write(innerFormula.Index);
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

		public static Formula ReadFormula(BinaryReader reader, Dictionary<int,Fault> indexToFault)
		{
			var type = reader.ReadInt32();
			if (type==1)
			{
				var label = reader.ReadString();
				var from = reader.ReadInt32();
				var to = reader.ReadInt32();
				return new SimpleStateInRangeFormula(from,to,label);

			}
			else if (type == 2)
			{
				var label = reader.ReadString();
				var index = reader.ReadInt32();
				var from = reader.ReadInt32();
				var to = reader.ReadInt32();
				return new SimpleLocalVarInRangeFormula(index,from, to, label);
			}
			else if (type == 3)
			{
				var label = reader.ReadString();
				var index = reader.ReadInt32();
				return new SimpleLocalVarIsTrue(index, label);
			}
			else if (type == 4)
			{
				var label = reader.ReadString();
				var index = reader.ReadInt32();
				return new FaultFormula(indexToFault[index], label);
			}
			else if (type == 5)
			{
				var label = reader.ReadString();
				var @operator = (UnaryOperator)reader.ReadInt32();
				var operand = ReadFormula(reader, indexToFault);
				return new UnaryFormula(operand, @operator,label);
			}
			else if (type == 6)
			{
				var label = reader.ReadString();
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

		public static byte[] SerializeToByteArray(SimpleModelBase model, Formula[] formulas)
		{
			Requires.NotNull(model, nameof(model));

			using (var buffer = new MemoryStream())
			using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
			{
				// write C# type of model
				var exactTypeOfModel = model.GetType();
				var exactTypeOfModelName = exactTypeOfModel.AssemblyQualifiedName;
				Requires.NotNull(exactTypeOfModelName, $"{exactTypeOfModelName} != null");
				writer.Write(exactTypeOfModelName);

				// write state
				writer.Write(model.State);

				// write formulas
				writer.Write((uint) formulas.Length);
				var faultToIndex = Enumerable.Range(0, model.Faults.Length).ToDictionary(x => model.Faults[x], x => x);
				foreach (var formula in formulas)
				{
					WriteFormula(writer, formula, faultToIndex);
				}

				// return result as array
				return buffer.ToArray();
			}
		}

		public static Tuple<SimpleModelBase,Formula[]> DeserializeFromByteArray(byte[] serializedModel)
		{
			Requires.NotNull(serializedModel, nameof(serializedModel));

			using (var buffer = new MemoryStream(serializedModel))
			using (var reader = new BinaryReader(buffer, Encoding.UTF8, leaveOpen: true))
			{
				// read C# type of model
				var exactTypeOfModelName = reader.ReadString();

				// read state and instantiate model
				var state = reader.ReadInt32();
				var exactTypeOfModel = Type.GetType(exactTypeOfModelName);
				Requires.NotNull(exactTypeOfModel, $"{exactTypeOfModel} != null");
				var deserializedModel = (SimpleModelBase)Activator.CreateInstance(exactTypeOfModel);
				deserializedModel.State = state;

				// read formulas
				var formulaNumber = reader.ReadUInt32();
				var formulas = new Formula[formulaNumber];
				var indexToFault = Enumerable.Range(0, deserializedModel.Faults.Length).ToDictionary(x => x, x => deserializedModel.Faults[x]);
				for (var i = 0; i < formulaNumber; ++i)
				{
					formulas[i] = ReadFormula(reader, indexToFault);
				}

				//return tuple of model and formulas
				return new Tuple<SimpleModelBase, Formula[]>(deserializedModel,formulas);
			}
		}

		public static SerializationDelegate CreateFastInPlaceDeserializer(SimpleModelBase model)
		{
			var permanentFaults = model.Faults.OfType<PermanentFault>().ToArray();

			var isActiveField = typeof(PermanentFault).GetField("_isActive", BindingFlags.NonPublic | BindingFlags.Instance);

			SerializationDelegate deserialize = state =>
			{
				// States
				model.State = *((int*)state+0);

				// Faults
				var faultsSerialized = *(long*)(state+sizeof(int));
				for (var i = 0; i < permanentFaults.Length; ++i)
				{
					var fault = permanentFaults[i];
					if ((faultsSerialized & (1L << i)) != 0)
					{
						isActiveField.SetValue(fault,true);
					}
					else
					{
						isActiveField.SetValue(fault, false);
					}
				}
			};
			return deserialize;
		}


		public static SerializationDelegate CreateFastInPlaceSerializer(SimpleModelBase model)
		{
			var permanentFaults = model.Faults.OfType<PermanentFault>().ToArray();

			var isActiveField = typeof(PermanentFault).GetField("_isActive", BindingFlags.NonPublic | BindingFlags.Instance);

			SerializationDelegate serialize = state =>
			{
				// States
				*((int*)state) = model.State;

				// Faults
				var faultsSerialized = 0L;
				for (var i = 0; i < permanentFaults.Length; ++i)
				{
					var fault = permanentFaults[i];
					var isActive = (bool) isActiveField.GetValue(fault);
					if (isActive)
					{
						faultsSerialized |= 1L << i;
					}
				}
				*(long*)(state + sizeof(int)) = faultsSerialized;
			};
			return serialize;
		}
	}
}
