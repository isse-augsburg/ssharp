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

namespace Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;
	using SafetySharp.Runtime.Serialization;
	using SafetySharp.Utilities;
	using Utilities;
	using Xunit.Abstractions;

	public abstract unsafe class SerializationObject : TestObject, IDisposable
	{
		private SerializationDelegate _deserializer;
		private ObjectTable _objectTable;
		private SerializationDelegate _serializer;
		protected byte* SerializedState { get; private set; }
		protected int StateSlotCount { get; private set; }
		protected int StateVectorSize { get; private set; }
		protected StateVectorLayout StateVectorLayout { get; private set; }
		private MemoryBuffer _buffer;

		public void Dispose()
		{
			_buffer.SafeDispose();
		}

		protected void GenerateCode(SerializationMode mode, params object[] objects)
		{
			objects = objects.SelectMany(obj => SerializationRegistry.Default.GetReferencedObjects(obj, mode)).ToArray();

			_objectTable = new ObjectTable(objects);
			StateVectorLayout = SerializationRegistry.Default.GetStateVectorLayout(_objectTable, mode);
			_serializer = StateVectorLayout.CreateSerializer();
			_deserializer = StateVectorLayout.CreateDeserializer();

			StateSlotCount = StateVectorLayout.SizeInBytes / 4;
			StateVectorSize = StateVectorLayout.SizeInBytes;
			_buffer = new MemoryBuffer();
			_buffer.Resize(StateVectorSize, zeroMemory: true);
			SerializedState = _buffer.Pointer;

			Output.Log("{0}", StateVectorLayout);
		}

		protected void Serialize()
		{
			_buffer.CheckBounds();
			_serializer(SerializedState);
			_buffer.CheckBounds();
		}

		protected void Deserialize()
		{
			_deserializer(SerializedState);
		}
	}

	public partial class SerializationTests : Tests
	{
		public SerializationTests(ITestOutputHelper output)
			: base(output)
		{
		}

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests(string directory)
		{
			return EnumerateTestCases(GetAbsoluteTestsDirectory(directory));
		}
	}
}