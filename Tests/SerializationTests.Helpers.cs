// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using JetBrains.Annotations;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;
	using Utilities;
	using Xunit.Abstractions;

	internal abstract class RuntimeModelTest : TestObject
	{
		private RuntimeModel _runtimeModel;

		protected Component[] RootComponents => _runtimeModel.RootComponents;

		protected StateFormula[] StateFormulas => _runtimeModel.StateFormulas;

		protected int StateSlotCount => _runtimeModel.StateSlotCount;

		protected void Create(Model model, params Formula[] formulas)
		{
			using (var memoryStream = new MemoryStream())
			{
				RuntimeModelSerializer.Save(memoryStream, model, formulas);

				memoryStream.Seek(0, SeekOrigin.Begin);
				_runtimeModel = RuntimeModelSerializer.Load(memoryStream);
			}
		}
	}

	public abstract unsafe class SerializationObject : TestObject
	{
		private SerializationDelegate _deserializer;
		private ObjectTable _objectTable;
		private int* _serializedState;
		private SerializationDelegate _serializer;
		private StateCache _stateCache;
		protected int _stateSlotCount;

		protected void GenerateCode(SerializationMode mode, params object[] objects)
		{
			objects = objects.SelectMany(obj => SerializationRegistry.Default.GetReferencedObjects(obj, mode)).ToArray();

			_objectTable = new ObjectTable(objects);
			_serializer = SerializationRegistry.Default.CreateStateSerializer(_objectTable, mode);
			_deserializer = SerializationRegistry.Default.CreateStateDeserializer(_objectTable, mode);

			_stateSlotCount = SerializationRegistry.Default.GetStateSlotCount(_objectTable, mode);
			_stateCache = new StateCache(_stateSlotCount + 1, 1);
			_serializedState = _stateCache.Allocate();
		}

		protected void Serialize()
		{
			_serializedState[_stateSlotCount].ShouldBe(0);
			_serializer(_serializedState);
			_serializedState[_stateSlotCount].ShouldBe(0, "Detected out-of-bounds memory access.");
		}

		protected void Deserialize()
		{
			_deserializer(_serializedState);
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