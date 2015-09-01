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

namespace SafetySharp.Runtime.Serialization
{
	using System.Collections.Generic;
	using System.Linq;
	using Utilities;

	/// <summary>
	///   Represents a registry of <see cref="ISerializer" />s.
	/// </summary>
	public sealed class SerializationRegistry
	{
		/// <summary>
		///   The default serializer that can serialize all objects.
		/// </summary>
		private readonly ObjectSerializer _objectSerializer = new ObjectSerializer();

		/// <summary>
		///   The list of registered serializers.
		/// </summary>
		private readonly List<ISerializer> _serializers = new List<ISerializer>();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		internal SerializationRegistry()
		{
		}

		/// <summary>
		///   Registers the <paramref name="serializer" />.
		/// </summary>
		/// <param name="serializer">The serializer that should be registered.</param>
		public void RegisterSerializer(ISerializer serializer)
		{
			Requires.NotNull(serializer, nameof(serializer));
			Requires.That(!_serializers.Contains(serializer), nameof(serializer),
				$"The serializer '{serializer.GetType().FullName}' has already been registered.");

			_serializers.Add(serializer);
		}

		/// <summary>
		///   Tries to find a serialize that is able to serialize the <paramref name="obj" />.
		/// </summary>
		private ISerializer FindSerializer(object obj)
		{
			var type = obj.GetType();
			var serializer = _serializers.FirstOrDefault(s => s.CanSerialize(type));

			return serializer ?? _objectSerializer;
		}

		/// <summary>
		///   Gets the number of state slots required by the serialized data of the <paramref name="objects" />.
		/// </summary>
		/// <param name="objects">The objects consisting of state values that should be serialized.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		internal int GetStateSlotCount(ObjectTable objects, SerializationMode mode)
		{
			return objects.Select(obj => FindSerializer(obj).GetStateSlotCount(obj, mode)).Sum();
		}

		/// <summary>
		///   Dynamically generates a delegate that can be used to serialize the <paramref name="objects" />.
		/// </summary>
		/// <param name="objects">The objects consisting of state values that should be serialized.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		internal unsafe SerializationDelegate GenerateSerializationDelegate(ObjectTable objects, SerializationMode mode)
		{
			var generator = new SerializationGenerator("Serialize");

			foreach (var obj in objects)
				FindSerializer(obj).Serialize(generator, obj, objects.GetObjectIdentifier(obj), mode);

			return generator.Compile(objects);
		}

		/// <summary>
		///   Dynamically generates a delegate that can be used to deserialize the <paramref name="objects" />.
		/// </summary>
		/// <param name="objects">The objects consisting of state values that should be deserialized.</param>
		/// <param name="mode">The serialization mode that should be used to deserialize the objects.</param>
		internal unsafe SerializationDelegate GenerateDeserializationDelegate(ObjectTable objects, SerializationMode mode)
		{
			var generator = new SerializationGenerator("Deserialize");

			foreach (var obj in objects)
				FindSerializer(obj).Deserialize(generator, obj, objects.GetObjectIdentifier(obj), mode);

			return generator.Compile(objects);
		}
	}
}