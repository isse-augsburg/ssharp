// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

namespace SafetySharp.Analysis
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Text;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Represents a model checking counter example.
	/// </summary>
	public abstract class CounterExampleSerialization<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		/// <summary>
		///   The file extension used by counter example files.
		/// </summary>
		public virtual string FileExtension { get; } = ".ssharp";

		/// <summary>
		///   The first few bytes that indicate that a file is a valid S# counter example file.
		/// </summary>
		public const int FileHeader = 0x3FE0DD04;
		
		internal CounterExampleSerialization()
		{
		}

		public abstract void WriteInternalStateStructure(CounterExample<TExecutableModel> counterExample, BinaryWriter writer);

		public abstract void ReadInternalStateStructure(BinaryReader reader);

		/// <summary>
		///   Saves the counter example to the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The file the counter example should be saved to.</param>
		public virtual void Save(CounterExample<TExecutableModel> counterExample, string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));

			if (!file.EndsWith(FileExtension))
				file += FileExtension;

			var directory = Path.GetDirectoryName(file);
			if (!String.IsNullOrWhiteSpace(directory))
				Directory.CreateDirectory(directory);

			using (var writer = new BinaryWriter(File.OpenWrite(file), Encoding.UTF8))
			{
				writer.Write(FileHeader);
				writer.Write(counterExample.EndsWithException);
				writer.Write(counterExample.RuntimeModel.SerializedModel.Length);
				writer.Write(counterExample.RuntimeModel.SerializedModel);

				foreach (var fault in counterExample.RuntimeModel.Faults)
					writer.Write((int)fault.Activation);

				WriteInternalStateStructure(counterExample,writer);

				writer.Write(counterExample.StepCount + 1);
				writer.Write(counterExample.RuntimeModel.StateVectorSize);

				foreach (var slot in counterExample.States.SelectMany(step => step))
					writer.Write(slot);

				writer.Write(counterExample.ReplayInfo.Length);
				foreach (var choices in counterExample.ReplayInfo)
				{
					writer.Write(choices.Length);
					foreach (var choice in choices)
						writer.Write(choice);
				}
			}
		}

		public abstract CounterExample<TExecutableModel> Load(string file);
	}
}