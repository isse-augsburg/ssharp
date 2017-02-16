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
	using System.IO;
	using JetBrains.Annotations;
	using Microsoft.Build.Utilities;
	using SafetySharp.Utilities;
	using Utilities;
	using Xunit.Abstractions;

	public abstract class End2EndTestObject : TestObject
	{
		private string _directory;

		protected sealed override void Check()
		{
			try
			{
				_directory = Guid.NewGuid().ToString();
				Directory.CreateDirectory(_directory);

				Run();
			}
			finally
			{
				Directory.Delete(_directory, true);
			}
		}

		protected abstract void Run();

		protected bool Compile(string testFile)
		{
			var projectPath = Path.Combine(_directory, "TestProject.csproj");

			File.Copy("End2End/Files/TestProject.csproj", projectPath, overwrite: true);
			File.Copy(Path.Combine("End2End/Files", testFile), Path.Combine(_directory, "TestCode.cs"), overwrite: true);

			var msbuildPath = ToolLocationHelper.GetPathToBuildTools(ToolLocationHelper.CurrentToolsVersion);

			var process = new ExternalProcess(Path.Combine(msbuildPath, "msbuild.exe"),
				$"\"{projectPath}\" /p:Configuration=Release /p:Platform=AnyCPU /nr:false",
				message => Output.Log("{0}", message));

			process.Run();
			return process.ExitCode == 0;
		}

		protected bool Execute()
		{
			File.Copy("SafetySharp.Modeling.dll", Path.Combine(_directory, "Binaries/Release/SafetySharp.Modeling.dll"), overwrite: true);
			File.Copy("ISSE.SafetyChecking.dll", Path.Combine(_directory, "Binaries/Release/ISSE.SafetyChecking.dll"), overwrite: true);

			var process = new ExternalProcess(Path.Combine(_directory, "Binaries/Release/Test.exe"), "");
			process.Run();

			return process.ExitCode == 0;
		}
	}

	public partial class End2EndTests : Tests
	{
		public End2EndTests(ITestOutputHelper output)
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