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
	using System.IO;
	using System.Reflection;
	using System.Threading;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Provides information about a <see cref="Model" /> that should be model checked.
	/// </summary>
	internal class ModelInfo
	{
		/// <summary>
		///   The name of the method or property generating the formula that should be checked.
		/// </summary>
		private string _formulaFactory;

		/// <summary>
		///   The thread-local model instance.
		/// </summary>
		private ThreadLocal<Model> _model;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		private ModelInfo()
		{
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the instance should be created for.</param>
		/// <param name="checkKind">Indicates which kind of check should be performed on a model.</param>
		/// <param name="formula">The name of the method or the property generating the formula that should be checked.</param>
		internal ModelInfo(Model model, CheckKind checkKind, string formula)
		{
			Requires.NotNull(model, nameof(model));
			Requires.InRange(checkKind, nameof(checkKind));
			Requires.NotNullOrWhitespace(formula, nameof(formula));
			Requires.That(model.GetType() != typeof(Model), nameof(model), $"Only models derived from '{typeof(Model).FullName}' are supported.");

			_model = new ThreadLocal<Model>(() => model);
			_formulaFactory = formula;
			CheckKind = checkKind;

			if (checkKind == CheckKind.Invariant)
				InvariantStateLabel = "invariant" + Guid.NewGuid().ToString().Replace("-", "");
		}

		/// <summary>
		///   Gets the model that should be checked.
		/// </summary>
		public Model Model => _model.Value;

		/// <summary>
		///   Gets a value indicating which kind of check should be performed on a model.
		/// </summary>
		public CheckKind CheckKind { get; private set; }

		/// <summary>
		///   Gets the name of the state label that should be checked for when doing reachability analysis.
		/// </summary>
		public string InvariantStateLabel { get; private set; }

		/// <summary>
		///   Save the model information to the <paramref name="file" />.
		/// </summary>
		/// <param name="file">The file the model information should be saved to.</param>
		internal void Save(string file)
		{
			Requires.NotNullOrWhitespace(file, nameof(file));

			File.WriteAllLines(file, new[]
			{
				Model.GetType().Assembly.Location,
				Model.GetType().FullName,
				CheckKind.ToString(),
				_formulaFactory,
				InvariantStateLabel ?? String.Empty
			});
		}

		/// <summary>
		///   Loads the model information from the <paramref name="modelFile" />.
		/// </summary>
		/// <param name="modelFile">The file containing the model information.</param>
		internal static ModelInfo Load(string modelFile)
		{
			Requires.NotNullOrWhitespace(modelFile, nameof(modelFile));

			var info = File.ReadAllLines(modelFile);
			Assert.That(info.Length == 5, "Unsupported model file.");

			var modelType = Assembly.LoadFile(info[0]).GetType(info[1]);

			return new ModelInfo
			{
				_formulaFactory = info[3],
				_model = new ThreadLocal<Model>(() =>
				{
					var model = (Model)Activator.CreateInstance(modelType);
					return model;
				}),
				CheckKind = (CheckKind)Enum.Parse(typeof(CheckKind), info[2]),
				InvariantStateLabel = info[4]
			};
		}
	}
}