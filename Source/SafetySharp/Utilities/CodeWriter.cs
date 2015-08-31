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

namespace SafetySharp.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using JetBrains.Annotations;

	/// <summary>
	///   Writes code with a C-like syntax to an in-memory buffer.
	/// </summary>
	public sealed class CodeWriter
	{
		/// <summary>
		///   The buffer that contains the written code.
		/// </summary>
		private readonly StringBuilder _buffer = new StringBuilder(4096);

		/// <summary>
		///   Indicates whether the writer is currently at the beginning of a new line.
		/// </summary>
		private bool _atBeginningOfLine = true;

		/// <summary>
		///   The number of tabs that are placed at the beginning of the next line.
		/// </summary>
		private int _indent;

		/// <summary>
		///   Appends the given format string to the current line.
		/// </summary>
		/// <param name="format">The format string that should be appended.</param>
		/// <param name="arguments">The arguments that should be copied into the format string.</param>
		[StringFormatMethod("format")]
		public void Append(string format, params object[] arguments)
		{
			AddIndentation();
			_buffer.AppendFormat(format, arguments);
		}

		/// <summary>
		///   Appends the given format string to the current line and starts a new line.
		/// </summary>
		/// <param name="format">The format string that should be appended.</param>
		/// <param name="arguments">The arguments that should be copied into the format string.</param>
		[StringFormatMethod("format")]
		public void AppendLine(string format, params object[] arguments)
		{
			AddIndentation();
			_buffer.AppendFormat(format, arguments);
			NewLine();
		}

		/// <summary>
		///   Appends a new line to the buffer.
		/// </summary>
		public void NewLine()
		{
			_buffer.AppendLine();
			_atBeginningOfLine = true;
		}

		/// <summary>
		///   Ensures that the subsequent write operation is performed on a new line.
		/// </summary>
		public void EnsureNewLine()
		{
			if (!_atBeginningOfLine)
				NewLine();
		}

		/// <summary>
		///   If the writer is currently at the beginning of a new line, adds the necessary number of tabs to the current line in
		///   order to get the desired indentation level.
		/// </summary>
		private void AddIndentation()
		{
			if (!_atBeginningOfLine)
				return;

			_atBeginningOfLine = false;
			for (var i = 0; i < _indent; ++i)
				_buffer.Append("\t");
		}

		/// <summary>
		///   Increases the indent.
		/// </summary>
		public void IncreaseIndent()
		{
			++_indent;
		}

		/// <summary>
		///   Decreases the indent.
		/// </summary>
		public void DecreaseIndent()
		{
			--_indent;
		}

		/// <summary>
		///   Appends a block statement to the buffer.
		/// </summary>
		/// <param name="content">
		///   Generates the content that should be placed within the block statement by calling Append methods
		///   of this code writer instance.
		/// </param>
		/// <param name="terminateWithSemicolon">Indicates whether the closing brace should be followed by a semicolon.</param>
		public void AppendBlockStatement(Action content, bool terminateWithSemicolon = false)
		{
			if (terminateWithSemicolon)
				AppendBlockStatement(content, ";");
			else
				AppendBlockStatement(content, "");
		}

		/// <summary>
		///   Generates a visibility modifier for the class.
		/// </summary>
		/// <param name="modifier">The modifier that should be generated.</param>
		public void GenerateVisibilityModifier(string modifier)
		{
			DecreaseIndent();
			AppendLine("{0}:", modifier);
			IncreaseIndent();
		}

		/// <summary>
		///   Appends a block statement to the buffer.
		/// </summary>
		/// <param name="content">
		///   Generates the content that should be placed within the block statement by calling Append methods
		///   of this code writer instance.
		/// </param>
		/// <param name="terminator">Terminates the the block statement.</param>
		/// <param name="args">The arguments that should be used to format the terminator.</param>
		[StringFormatMethod("terminator")]
		public void AppendBlockStatement(Action content, string terminator, params object[] args)
		{
			Requires.NotNull(content, nameof(content));

			EnsureNewLine();
			AppendLine("{{");
			IncreaseIndent();

			content();

			EnsureNewLine();
			DecreaseIndent();
			Append("}}");

			Append(terminator, args);
			NewLine();
		}

		/// <summary>
		///   Appends a list of values to the current line, with each value being separated by the given separator.
		/// </summary>
		/// <param name="source">The source values, for each of which the content is generated.</param>
		/// <param name="separator">The separator that separates to successive values.</param>
		/// <param name="content">
		///   Generates the content that should be appended for each value in source by calling Append methods
		///   of this code writer instance.
		/// </param>
		public void AppendSeparated<T>(IEnumerable<T> source, string separator, Action<T> content)
		{
			AppendSeparated(source, () => Append(separator), content);
		}

		/// <summary>
		///   Appends a list of values to the current line, with each value being separated by the given separator.
		/// </summary>
		/// <param name="source">The source values, for each of which the content is generated.</param>
		/// <param name="separator">
		///   Generates the separator that should be appended between two consecutive values by calling Append methods
		///   of this code writer instance.
		/// </param>
		/// <param name="content">
		///   Generates the content that should be appended for each value in source by calling Append methods
		///   of this code writer instance.
		/// </param>
		public void AppendSeparated<T>(IEnumerable<T> source, Action separator, Action<T> content)
		{
			Requires.NotNull(source, nameof(source));
			Requires.NotNull(separator, nameof(separator));
			Requires.NotNull(content, nameof(content));

			var count = source.Count();
			var i = 0;
			foreach (var value in source)
			{
				content(value);
				if (i < count - 1)
					separator();

				++i;
			}
		}

		/// <summary>
		///   Returns a string that represents the current object.
		/// </summary>
		public override string ToString()
		{
			return _buffer.ToString();
		}

		/// <summary>
		///   Removes all generated code from the writer.
		/// </summary>
		public void Clear()
		{
			_buffer.Clear();
		}
	}
}