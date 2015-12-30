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

namespace SafetySharp.Compiler.Normalization
{
	using System;
	using Microsoft.CodeAnalysis;
	using Roslyn.Syntax;

	/// <summary>
	///   The S# compiler changes the C# code behind the modeler's back. To ensure that the debugging experience is not negatively
	///   influenced, this normalizer adds a <c>#line</c> pragma directive to the beginning of each file that redirects the
	///   debugger to the original source file. Also, it changes the name of the compiled file so that the PDB does not reference
	///   the actual file that is on-disk (there would be a checksum mismatch and debugging would not work properly).
	/// </summary>
	public sealed class LineDirectiveNormalizer : Normalizer
	{
		/// <summary>
		///   Normalizes the <paramref name="syntaxTree" /> of the <see cref="Compilation" />.
		/// </summary>
		/// <param name="syntaxTree">The syntax tree that should be normalized.</param>
		protected override SyntaxTree Normalize(SyntaxTree syntaxTree)
		{
			var root = syntaxTree.GetRoot();
			var firstNode = root.GetFirstToken(true, true, true, true).Parent;
			var updatedNode = firstNode.PrependLineDirective(1, syntaxTree.FilePath);

			return syntaxTree.WithFilePath(syntaxTree.FilePath + Guid.NewGuid()).WithRoot(root.ReplaceNode(firstNode, updatedNode));
		}
	}
}