// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// Copyright (c) 2017, Manuel Götz
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


using System;
using System.IO;
using ISSE.SafetyChecking.ExecutableModel;
using ISSE.SafetyChecking.Utilities;

namespace SafetyLustre
{
	using System.Linq;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Text;
	using ISSE.SafetyChecking.Modeling;

	public class LustreExecutableModelCounterExampleSerialization : CounterExampleSerialization<LustreExecutableModel>
	{
		public override void WriteInternalStateStructure(ExecutableCounterExample<LustreExecutableModel> counterExample, BinaryWriter writer)
		{
		}

		/// <summary>
		///   Loads a counter example from the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The path to the file the counter example should be loaded from.</param>
		public override ExecutableCounterExample<LustreExecutableModel> Load(string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));

			using (var reader = new BinaryReader(File.OpenRead(file), Encoding.UTF8))
			{
				if (reader.ReadInt32() != FileHeader)
					throw new InvalidOperationException("The file does not contain a counter example that is compatible with this version of S#.");

				var endsWithException = reader.ReadBoolean();
				var serializedRuntimeModel = reader.ReadBytes(reader.ReadInt32());
				
				var runtimeModel = new LustreExecutableModel(serializedRuntimeModel);
				
				runtimeModel.UpdateFaultSets();

				var faultsLength = reader.ReadInt32();
				
				var counterExample = new byte[reader.ReadInt32()][];
				var slotCount = reader.ReadInt32();

				if (slotCount != runtimeModel.StateVectorSize)
				{
					throw new InvalidOperationException(
						$"State slot count mismatch; the instantiated model requires {runtimeModel.StateVectorSize} state slots, " +
						$"whereas the counter example uses {slotCount} state slots.");
				}
				

				for (var i = 0; i < counterExample.Length; ++i)
				{
					counterExample[i] = new byte[runtimeModel.StateVectorSize];
					for (var j = 0; j < runtimeModel.StateVectorSize; ++j)
						counterExample[i][j] = reader.ReadByte();
				}

				var replayInfo = new int[reader.ReadInt32()][];
				for (var i = 0; i < replayInfo.Length; ++i)
				{
					replayInfo[i] = new int[reader.ReadInt32()];
					for (var j = 0; j < replayInfo[i].Length; ++j)
						replayInfo[i][j] = reader.ReadInt32();
				}

				var faultActivations = runtimeModel.Faults.Select(fault => fault.Activation).ToArray();

				return new ExecutableCounterExample<LustreExecutableModel>(runtimeModel, counterExample, replayInfo, endsWithException, faultActivations);
			}
		}
	}
}