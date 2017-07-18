using System;
using System.IO;
using ISSE.SafetyChecking.ExecutableModel;
using ISSE.SafetyChecking.Utilities;

namespace Tests.SimpleExecutableModel
{
	using System.Linq;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Text;
	using ISSE.SafetyChecking.Modeling;

	public class SimpleExecutableModelCounterExampleSerialization : CounterExampleSerialization<SimpleExecutableModel>
	{
		public override void WriteInternalStateStructure(ExecutableCounterExample<SimpleExecutableModel> counterExample, BinaryWriter writer)
		{
			// write meta data to validate that the right model was loaded
			writer.Write(counterExample.RuntimeModel.Model.Faults.Length);
			writer.Write(counterExample.RuntimeModel.Model.LocalBools.Length);
			writer.Write(counterExample.RuntimeModel.Model.LocalInts.Length);
		}

		/// <summary>
		///   Loads a counter example from the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The path to the file the counter example should be loaded from.</param>
		public override ExecutableCounterExample<SimpleExecutableModel> Load(string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));

			using (var reader = new BinaryReader(File.OpenRead(file), Encoding.UTF8))
			{
				if (reader.ReadInt32() != FileHeader)
					throw new InvalidOperationException("The file does not contain a counter example that is compatible with this version of S#.");

				var endsWithException = reader.ReadBoolean();
				var serializedRuntimeModel = reader.ReadBytes(reader.ReadInt32());
				
				var runtimeModel = new SimpleExecutableModel(serializedRuntimeModel);

				foreach (var fault in runtimeModel.Faults.Where(fault => fault.IsUsed))
					fault.Activation = (Activation)reader.ReadInt32();

				runtimeModel.UpdateFaultSets();

				var faultsLength = reader.ReadInt32();
				var localBoolsLength = reader.ReadInt32();
				var localIntsLength = reader.ReadInt32();
				
				var counterExample = new byte[reader.ReadInt32()][];
				var slotCount = reader.ReadInt32();

				if (slotCount != runtimeModel.StateVectorSize)
				{
					throw new InvalidOperationException(
						$"State slot count mismatch; the instantiated model requires {runtimeModel.StateVectorSize} state slots, " +
						$"whereas the counter example uses {slotCount} state slots.");
				}

				if (runtimeModel.Model.Faults.Length != faultsLength || runtimeModel.Model.LocalBools.Length != localBoolsLength || runtimeModel.Model.LocalInts.Length != localIntsLength)
				{
					throw new InvalidOperationException(
						"The serialized model does not match to the current model. Maybe the model was changed and does not fit to the saved version.");
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

				return new ExecutableCounterExample<SimpleExecutableModel>(runtimeModel, counterExample, replayInfo, endsWithException, faultActivations);
			}
		}
	}
}