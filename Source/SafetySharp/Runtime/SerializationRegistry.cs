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

namespace SafetySharp.Runtime
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using Modeling;
	using Modeling.Faults;
	using Utilities;

	/// <summary>
	///   Represents a registry of <see cref="ObjectStateSerializer" />s.
	/// </summary>
	public sealed class SerializationRegistry
	{
		/// <summary>
		///   Gets the fields of the <paramref name="obj" /> that have to be serialized.
		/// </summary>
		/// <param name="obj">The object that should be serialized.</param>
		public delegate IEnumerable<FieldInfo> ObjectStateSerializer(object obj);

		/// <summary>
		///   The map of registered serializers.
		/// </summary>
		private readonly Dictionary<Type, ObjectStateSerializer> _serializers = new Dictionary<Type, ObjectStateSerializer>();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the registry should belong to.</param>
		internal SerializationRegistry(Model model)
		{
			Requires.NotNull(model, nameof(model));

			Model = model;

			RegisterType(typeof(Component), obj => GetComponentFields((Component)obj));
			RegisterType(typeof(Fault), obj => GetFaultFields((Fault)obj));
			RegisterType(typeof(OccurrencePattern), obj => GetOccurrencePatternFields((OccurrencePattern)obj));
		}

		/// <summary>
		///   Gets the <see cref="Model" /> the registry belongs to.
		/// </summary>
		public Model Model { get; }

		/// <summary>
		///   Registers the state <paramref name="serializer" /> for the <paramref name="type" />.
		/// </summary>
		/// <param name="type">The type the <paramref name="serializer" /> should be registered for.</param>
		/// <param name="serializer">The serializer that should be registered for the <paramref name="type" />.</param>
		public void RegisterType(Type type, ObjectStateSerializer serializer)
		{
			Requires.NotNull(type, nameof(type));
			Requires.That(!_serializers.ContainsKey(type), nameof(type), $"The type '{type.FullName}' has already been registered.");
			Requires.NotNull(serializer, nameof(serializer));

			_serializers.Add(type, serializer);
		}

		/// <summary>
		///   Gets the number of state slots required by the serialized data of <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">The object consisting of state values that should be serialized.</param>
		internal int GetStateSlotCount(object obj)
		{
			return GetFields(obj).Sum(field =>
			{
				var fieldType = field.FieldType.IsEnum ? field.FieldType.GetEnumUnderlyingType() : field.FieldType;
				Assert.That(fieldType.IsPrimitive, $"Expected a field of primitive type; type '{fieldType.FullName}' is unsupported.");

				return (Marshal.SizeOf(fieldType) + sizeof(int) - 1) / sizeof(int);
			});
		}

		/// <summary>
		///   Dynamically generates a delegate that can be used to serialize the <see cref="Model" />.
		/// </summary>
		internal SerializationDelegate GenerateSerializationDelegate()
		{
			var generator = new SerializationGenerator(Model.ObjectTable, "Serialize");

			foreach (var obj in Model.ObjectTable.Objects)
				Model.SerializationRegistry.Serialize(generator, obj);

			return generator.Compile();
		}

		/// <summary>
		///   Dynamically generates a delegate that can be used to deserialize the <see cref="Model" />.
		/// </summary>
		internal SerializationDelegate GenerateDeserializationDelegate()
		{
			var generator = new SerializationGenerator(Model.ObjectTable, "Deserialize");

			foreach (var obj in Model.ObjectTable.Objects)
				Model.SerializationRegistry.Deserialize(generator, obj);

			return generator.Compile();
		}

		/// <summary>
		///   Generates the code that serializes the state values of the <paramref name="obj" />.
		/// </summary>
		/// <param name="generator">The generator that should be used to generate the serialization method.</param>
		/// <param name="obj">The object consisting of state values that should be serialized.</param>
		private void Serialize(SerializationGenerator generator, object obj)
		{
			Requires.NotNull(generator, nameof(generator));

			var identifier = generator.GetObjectIdentifier(obj);
			foreach (var field in GetFields(obj))
				generator.SerializeField(identifier, field);
		}

		/// <summary>
		///   Generates the code that deserializes the state values of the <paramref name="obj" />.
		/// </summary>
		/// <param name="generator">The generator that should be used to generate the deserialization method.</param>
		/// <param name="obj">The object consisting of state values that should be deserialized.</param>
		private void Deserialize(SerializationGenerator generator, object obj)
		{
			Requires.NotNull(generator, nameof(generator));

			var identifier = generator.GetObjectIdentifier(obj);
			foreach (var field in GetFields(obj))
				generator.DeserializeField(identifier, field);
		}

		/// <summary>
		///   Gets the fields declared by the <paramref name="component" /> whose values should be serialized.
		/// </summary>
		/// <param name="component">The component the fields should be returned for.</param>
		private static IEnumerable<FieldInfo> GetComponentFields(Component component)
		{
			return Enumerable.Empty<FieldInfo>();
			//			var metadata = component.Metadata;
			//
			//			foreach (var field in metadata.Fields.Where(field => !field.FieldInfo.IsInitOnly).Select(field => field.FieldInfo))
			//				yield return field;
			//
			//			if (metadata.StateMachine != null)
			//				yield return metadata.StateMachine.StateField.FieldInfo;
		}

		/// <summary>
		///   Gets the fields declared by the <paramref name="fault" /> whose values should be serialized.
		/// </summary>
		/// <param name="fault">The component the fields should be returned for.</param>
		private static IEnumerable<FieldInfo> GetFaultFields(Fault fault)
		{
//			if (fault.IsIgnored)
			return Enumerable.Empty<FieldInfo>();

			//return fault.Metadata.Fields.Where(field => !field.FieldInfo.IsInitOnly).Select(field => field.FieldInfo);
		}

		/// <summary>
		///   Gets the fields declared by the <paramref name="occurrencePattern" /> whose values should be serialized.
		/// </summary>
		/// <param name="occurrencePattern">The component the fields should be returned for.</param>
		private static IEnumerable<FieldInfo> GetOccurrencePatternFields(OccurrencePattern occurrencePattern)
		{
//			if (occurrencePattern.Metadata.DeclaringFault.Fault.IsIgnored)
			return Enumerable.Empty<FieldInfo>();

			//return occurrencePattern.Metadata.Fields.Where(field => !field.FieldInfo.IsInitOnly).Select(field => field.FieldInfo);
		}

		/// <summary>
		///   Gets the <paramref name="obj" />'s fields that have to be serialized.
		/// </summary>
		/// <param name="obj">The object the fields should be returned for.</param>
		private IEnumerable<FieldInfo> GetFields(object obj)
		{
			ObjectStateSerializer serializer = null;
			var type = obj.GetType();

			while (type != null && !_serializers.TryGetValue(type, out serializer))
				type = type.BaseType;

			Assert.That(serializer != null, $"No serializer has been registered for type '{obj.GetType().FullName}'.");

			var fields = serializer(obj);
			return fields ?? Enumerable.Empty<FieldInfo>();
		}
	}
}