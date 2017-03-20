using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ISSE.SafetyChecking.ExecutableModel;
using ISSE.SafetyChecking.Modeling;
using ISSE.SafetyChecking.Utilities;

namespace Tests.SimpleExecutableModel
{
    public class SimpleExecutableModelCounterExampleSerialization : CounterExampleSerialization<SimpleExecutableModel>
    {
        public override void WriteInternalStateStructure(CounterExample<SimpleExecutableModel> counterExample, BinaryWriter writer)
        {
        }

        /// <summary>
        ///   Loads a counter example from the <paramref name="file" />.
        /// </summary>
        /// <param name="file">The path to the file the counter example should be loaded from.</param>
        public override CounterExample<SimpleExecutableModel> Load(string file)
        {
            Requires.NotNullOrWhitespace(file, nameof(file));
            throw new NotImplementedException();
        }
    }
}