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

namespace SafetySharp.Compiler
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using Microsoft.Build.Framework;
	using Microsoft.Build.Utilities;

	/// <summary>
	///   The S# code normalization task that is executed by MSBuild when compiling S# projects.
	/// </summary>
	public class NormalizationTask : Task
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public NormalizationTask()
		{
			AppDomain.CurrentDomain.AssemblyResolve += (s, e) => LoadAssembly(new AssemblyName(e.Name).Name);
		}

		/// <summary>
		///   The input files of the compiled project that should be compiled.
		/// </summary>
		[Required]
		public ITaskItem[] InputFiles { get; set; }

		/// <summary>
		///   The paths to the references that are included by the compiled project.
		/// </summary>
		[Required]
		public ITaskItem[] References { get; set; }

		/// <summary>
		///   The path of the intermediate directory the generated files should be written to.
		/// </summary>
		[Required]
		public ITaskItem IntermediateDirectory { get; set; }

		/// <summary>
		///   The paths where assemblies should be loaded from.
		/// </summary>
		[Required]
		public ITaskItem[] AssemblyDirectories { get; set; }

		/// <summary>
		///   The paths of the generated, normalized output files.
		/// </summary>
		[Output]
		public ITaskItem[] OutputFiles { get; set; }

		/// <summary>
		///   Executes the task, generating the normalized files for the inputs.
		/// </summary>
		public override bool Execute()
		{
			try
			{
				// Ensure the intermediate directory exists
				var outPath = IntermediateDirectory.GetMetadata("FullPath");
				var intermediateDirectory = Path.Combine(outPath, "ssharp");
				Directory.CreateDirectory(intermediateDirectory);

				// Normalize the code; a value of null indicates that the project contains errors
				var compilationUnits = Normalize();
				if (compilationUnits == null)
					return false;

				// Write the output files to disk and return the paths
				OutputFiles = new ITaskItem[compilationUnits.Length];
				for (var i = 0; i < compilationUnits.Length; ++i)
				{
					var path = Path.Combine(intermediateDirectory, i + ".cs");

					OutputFiles[i] = new TaskItem(path);
					File.WriteAllText(path, compilationUnits[i]);
				}

				return true;
			}
			catch (TargetInvocationException e)
			{
				ReportException(e.InnerException);
				return false;
			}
			catch (Exception e)
			{
				ReportException(e);
				return false;
			}
		}

		/// <summary>
		///   Reports the <paramref name="exception" />.
		/// </summary>
		private void ReportException(Exception exception)
		{
			Log.LogError("S# normalization failed due to an exception.");
			Log.LogError("{0}", exception.Message);
			Log.LogError("{0}", exception.StackTrace);
		}

		/// <summary>
		///   Tries to load the assembly with the <paramref name="assemblyName" /> from either the reference paths or the assembly
		///   directories. The assemblies are loaded to memory first in order to avoid locking the actual assemblies on disk.
		/// </summary>
		private Assembly LoadAssembly(string assemblyName)
		{
			var referencePath = References.FirstOrDefault(
				rp => string.Equals(rp.GetMetadata("FileName"), assemblyName, StringComparison.OrdinalIgnoreCase));

			if (referencePath != null)
				return Assembly.Load(File.ReadAllBytes(referencePath.GetMetadata("FullPath")));

			foreach (var directory in AssemblyDirectories)
			{
				string fileName = Path.Combine(directory.GetMetadata("FullPath"), assemblyName);

				if (File.Exists(fileName))
					return Assembly.Load(File.ReadAllBytes(fileName));
			}

			return null;
		}

		/// <summary>
		///   Normalizes the input files. The <see cref="Compiler.NormalizeProject" /> method is invoked via reflection in order to
		///   control assembly loading behavior so that the compiler assembly is not locked on disk.
		/// </summary>
		private string[] Normalize()
		{
			var assembly = LoadAssembly("SafetySharp.Compiler.dll");
			if (assembly == null)
				throw new InvalidOperationException("Unable to find the S# compiler.");

			var compilerType = assembly.GetType("SafetySharp.Compiler.Compiler");

			var compiler = Activator.CreateInstance(compilerType, new[] { Log });
			var method = compilerType.GetMethod("NormalizeProject", BindingFlags.Instance | BindingFlags.Public);
			var normalize = (Normalizer)Delegate.CreateDelegate(typeof(Normalizer), compiler, method);

			return normalize(InputFiles.Select(f => f.ItemSpec).ToArray(), References.Select(r => r.ItemSpec).ToArray(),
				IntermediateDirectory.ItemSpec);
		}

		/// <summary>
		///   A delegate representing the <see cref="Compiler.NormalizeProject" /> method.
		/// </summary>
		private delegate string[] Normalizer(string[] files, string[] references, string intermediateDirectory);
	}
}