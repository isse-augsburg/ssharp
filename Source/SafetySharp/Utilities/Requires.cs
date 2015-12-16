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
	using System.Collections;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using JetBrains.Annotations;

	/// <summary>
	///   Defines a set of helper functions that should be used to assert preconditions of functions.
	/// </summary>
	internal static class Requires
	{
		/// <summary>
		///   Throws an <see cref="ArgumentNullException" /> if <paramref name="argument" /> of reference type
		///   <typeparamref name="T" /> is <c>null</c>.
		/// </summary>
		/// <typeparam name="T">The type of the argument that should be checked.</typeparam>
		/// <param name="argument">The actual argument whose value should be checked.</param>
		/// <param name="argumentName">The name of the argument that is checked.</param>
		/// <exception cref="ArgumentNullException">
		///   Thrown if the value of <paramref name="argument" /> or <paramref name="argumentName" /> is <c>null</c>.
		/// </exception>
		[DebuggerHidden, ContractAnnotation("argument: null => halt")]
		public static void NotNull<T>([NoEnumeration] T argument, [NotNull] string argumentName)
			where T : class
		{
			NotNullOrWhitespace(argumentName, nameof(argumentName));

			if (argument == null)
				throw new ArgumentNullException(argumentName);
		}

		/// <summary>
		///   Throws an <see cref="ArgumentNullException" /> if <paramref name="argument" /> is <c>null</c>.
		/// </summary>
		/// <param name="argument">The actual argument whose value should be checked.</param>
		/// <param name="argumentName">The name of the argument that is checked.</param>
		/// <exception cref="ArgumentNullException">
		///   Thrown if the value of <paramref name="argument" /> or <paramref name="argumentName" /> is <c>null</c>.
		/// </exception>
		[DebuggerHidden, ContractAnnotation("argument: null => halt")]
		public static unsafe void NotNull(int* argument, [NotNull] string argumentName)
		{
			NotNullOrWhitespace(argumentName, nameof(argumentName));

			if (argument == null)
				throw new ArgumentNullException(argumentName);
		}

		/// <summary>
		///   Throws an <see cref="ArgumentNullException" /> if <paramref name="argument" /> is <c>null</c>. If
		///   <paramref name="argument" /> is empty or consists of whitespace only, an <see cref="ArgumentException" /> is thrown.
		/// </summary>
		/// <param name="argument">The actual argument whose value should be checked.</param>
		/// <param name="argumentName">The name of the argument that is checked.</param>
		/// <exception cref="ArgumentNullException">
		///   Thrown if the value of <paramref name="argument" /> or <paramref name="argumentName" /> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///   Thrown if the value of <paramref name="argument" /> is empty or consists of whitespace only.
		/// </exception>
		[DebuggerHidden, ContractAnnotation("argument: null => halt")]
		public static void NotNullOrWhitespace(string argument, [NotNull] string argumentName)
		{
			if (String.IsNullOrWhiteSpace(argumentName))
				throw new ArgumentException("The argument name cannot be empty or consist of whitespace only.", argumentName);

			if (String.IsNullOrWhiteSpace(argument))
				throw new ArgumentException("The argument cannot be empty or consist of whitespace only.", argumentName);
		}

		/// <summary>
		///   Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="argument" /> of enumeration type
		///   <typeparamref name="TEnum" /> is outside the range of valid enumeration literals. This method cannot be used to check
		///   enumeration literals if the <see cref="FlagsAttribute" /> is set on <typeparamref name="TEnum" />.
		/// </summary>
		/// <typeparam name="TEnum">The type of the enumeration argument that should be checked.</typeparam>
		/// <param name="argument">The actual enumeration value that should be checked.</param>
		/// <param name="argumentName">The name of the argument that is checked.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="argumentName" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">Thrown if <typeparamref name="TEnum" /> is not an enumeration type.</exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Thrown if the value of <paramref name="argument" /> is outside the range of valid enumeration literals.
		/// </exception>
		[DebuggerHidden]
		public static void InRange<TEnum>(TEnum argument, [NotNull] string argumentName)
			where TEnum : struct
		{
			NotNullOrWhitespace(argumentName, nameof(argumentName));

			if (!typeof(TEnum).IsEnum)
				throw new ArgumentException($"'{typeof(TEnum).FullName}' is not an enumeration type.", argumentName);

			if (!Enum.IsDefined(typeof(TEnum), argument))
				throw new ArgumentOutOfRangeException(argumentName, argument, "Enumeration parameter is out of range.");
		}

		/// <summary>
		///   Throws an <see cref="ArgumentOutOfRangeException" /> if the value of <paramref name="argument" /> of
		///   <see cref="IComparable" /> type <typeparamref name="T" /> is outside the range defined by the inclusive
		///   <paramref name="lowerBound" /> and the exclusive <paramref name="upperBound" />.
		/// </summary>
		/// <typeparam name="T">The type of the value to check.</typeparam>
		/// <param name="argument">The actual value that should be checked.</param>
		/// <param name="argumentName">The name of the argument that is checked.</param>
		/// <param name="lowerBound">The inclusive lower bound of the range.</param>
		/// <param name="upperBound">The exclusive upper bound of the range.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="argumentName" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">
		///   Thrown if <paramref name="lowerBound" /> does not precede <paramref name="upperBound" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Thrown if the value of <paramref name="argument" /> precedes <paramref name="lowerBound" /> or is
		///   the same as or exceeds <paramref name="upperBound" />.
		/// </exception>
		[DebuggerHidden]
		public static void InRange<T>(T argument, [NotNull] string argumentName, T lowerBound, T upperBound)
			where T : IComparable<T>
		{
			NotNullOrWhitespace(argumentName, nameof(argumentName));
			That(lowerBound.CompareTo(upperBound) <= 0, nameof(lowerBound), $"lowerBound '{lowerBound}' does not precede upperBound '{upperBound}'.");

			if (argument.CompareTo(lowerBound) < 0)
			{
				throw new ArgumentOutOfRangeException(argumentName, argument,
					$"Lower bound violation. Expected argument to lie between {lowerBound} and {upperBound}; actual value: {argument}.");
			}

			if (argument.CompareTo(upperBound) >= 0)
			{
				throw new ArgumentOutOfRangeException(argumentName, argument,
					$"Upper bound violation. Expected argument to lie between {lowerBound} and {upperBound}; actual value: {argument}.");
			}
		}

		/// <summary>
		///   Throws an <see cref="ArgumentOutOfRangeException" /> if the value of <paramref name="indexArgument" />
		///   falls outside the range of valid indices for <paramref name="collection" />.
		/// </summary>
		/// <param name="indexArgument">The actual index that should be checked.</param>
		/// <param name="argumentName">The name of the argument that is checked.</param>
		/// <param name="collection">The collection defining the range of valid indices.</param>
		/// <exception cref="ArgumentNullException">
		///   Thrown if the value of <paramref name="argumentName" /> or <paramref name="collection" /> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///   Thrown if the value of <paramref name="indexArgument" /> is smaller than 0 or exceeds <c>collection.Count</c>.
		/// </exception>
		[DebuggerHidden]
		public static void InRange(int indexArgument, [NotNull] string argumentName, ICollection collection)
		{
			NotNullOrWhitespace(argumentName, nameof(argumentName));
			NotNull(collection, nameof(collection));
			InRange(indexArgument, argumentName, 0, collection.Count);
		}

		/// <summary>
		///   Throws an <see cref="ArgumentException" /> if <paramref name="argument" /> is not of type
		///   <typeparamref name="T" />.
		/// </summary>
		/// <typeparam name="T">The desired type of <paramref name="argument" />.</typeparam>
		/// <param name="argument">The value whose type should be checked.</param>
		/// <param name="argumentName">The name of the argument that is checked.</param>
		/// <param name="message">An optional message providing further details about the assertion.</param>
		/// <exception cref="ArgumentNullException">
		///   Thrown if <paramref name="argument" /> or <paramref name="argumentName" /> is <c>null</c>.
		/// </exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="argument" /> is not of type <typeparamref name="T" />.</exception>
		[DebuggerHidden]
		public static void OfType<T>(object argument, [NotNull] string argumentName, string message = null)
			where T : class
		{
			NotNull(argument, nameof(argument));
			NotNullOrWhitespace(argumentName, nameof(argumentName));

			if (argument is T)
				return;

			message = message ??
					  $"Expected an instance of type '{typeof(T).FullName}' but found an instance of type '{argument.GetType().FullName}'.";
			throw new ArgumentException(message, argumentName);
		}

		/// <summary>
		///   Throws an <see cref="ArgumentException" /> if <paramref name="condition" /> is <c>false</c>.
		/// </summary>
		/// <param name="condition">The condition that, if <c>false</c>, causes the exception to be raised.</param>
		/// <param name="argumentName">The name of the argument that is checked.</param>
		/// <param name="message">A message providing further details about the assertion.</param>
		[DebuggerHidden, ContractAnnotation("condition: false => halt")]
		public static void That(bool condition, [NotNull] string argumentName, [NotNull] string message)
		{
			NotNullOrWhitespace(argumentName, nameof(argumentName));
			NotNullOrWhitespace(message, nameof(message));

			if (!condition)
				throw new ArgumentException(message, argumentName);
		}

		/// <summary>
		///   Throws an <see cref="InvalidOperationException" /> if <paramref name="condition" /> is <c>false</c>.
		/// </summary>
		/// <param name="condition">The condition that, if <c>false</c>, causes the exception to be raised.</param>
		/// <param name="message">A message providing further details about the assertion.</param>
		[DebuggerHidden, ContractAnnotation("condition: false => halt")]
		public static void That(bool condition, [NotNull] string message)
		{
			NotNullOrWhitespace(message, nameof(message));

			if (!condition)
				throw new InvalidOperationException(message);
		}

		/// <summary>
		///   Throws a <see cref="NotSupportedException" /> indicating that a compiler transformed version of the caller should be
		///   invoked instead.
		/// </summary>
		public static void CompilationTransformation([CallerMemberName] string caller = null)
		{
			throw new NotSupportedException($"Member '{caller}' cannot be called at runtime. Use the S# compiler to compile the assembly.");
		}
	}
}