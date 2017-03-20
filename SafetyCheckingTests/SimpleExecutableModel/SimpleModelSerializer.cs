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
    using System.Diagnostics;

    public static class SimpleModelSerializer
    {
        public static byte[] Serialize(SimpleModelBase model)
        {
            Requires.NotNull(model, nameof(model));

            using (var buffer = new MemoryStream())
            using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
            {
                var exactTypeOfModel = model.GetType();
                var exactTypeOfModelName = exactTypeOfModel.AssemblyQualifiedName;
                Requires.NotNull(exactTypeOfModelName, $"{exactTypeOfModelName} != null");
                writer.Write(exactTypeOfModelName);
                writer.Write(model.State);
                return buffer.ToArray();
            }
        }

        public static SimpleModelBase Deserialize(byte[] serializedModel)
        {
            Requires.NotNull(serializedModel, nameof(serializedModel));

            using (var buffer = new MemoryStream(serializedModel))
            using (var reader = new BinaryReader(buffer, Encoding.UTF8, leaveOpen: true))
            {
                var exactTypeOfModelName = reader.ReadString();
                var state = reader.ReadInt32();
                var exactTypeOfModel = Type.GetType(exactTypeOfModelName);
                Requires.NotNull(exactTypeOfModel, $"{exactTypeOfModel} != null");
                var deserializedModel = (SimpleModelBase)Activator.CreateInstance(exactTypeOfModel);
                deserializedModel.State = state;
                return deserializedModel;
            }
        }
    }
}
