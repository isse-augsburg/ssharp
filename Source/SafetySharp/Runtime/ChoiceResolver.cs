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

namespace SafetySharp.Runtime
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Modeling;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Represents a stack that is used to resolve nondeterministic choices during state space enumeration.
	/// </summary>
	[NonSerializable]
	internal sealed class ChoiceResolver : DisposableObject
	{
		/// <summary>
		///   The number of nondeterministic choices that can be stored initially.
		/// </summary>
		private const int InitialCapacity = 64;

		/// <summary>
		///   The stack that indicates the chosen values for the current path.
		/// </summary>
		private readonly ChoiceStack _chosenValues = new ChoiceStack(InitialCapacity);

		/// <summary>
		///   The stack that stores the number of possible values of all encountered choices along the current path.
		/// </summary>
		private readonly ChoiceStack _valueCount = new ChoiceStack(InitialCapacity);

		/// <summary>
		///   The number of choices that have been encountered for the current path.
		/// </summary>
		private int _choiceIndex = -1;

		/// <summary>
		///   Indicates whether the next path is the first one of the current state.
		/// </summary>
		private bool _firstPath;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="objectTable">The object table containing all objects that potentially require access to the choice resolver.</param>
		public ChoiceResolver(ObjectTable objectTable)
		{
			foreach (var obj in objectTable.OfType<Choice>())
				obj.Resolver = this;
		}

		/// <summary>
		///   Gets the index of the last choice that has been made.
		/// </summary>
		// ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
		internal int LastChoiceIndex => _choiceIndex;

		/// <summary>
		///   Prepares the resolver for resolving the choices of the next state.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PrepareNextState()
		{
			_firstPath = true;
		}

		/// <summary>
		///   Prepares the resolver for the next path. Returns <c>true</c> to indicate that all paths have been enumerated.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool PrepareNextPath()
		{
			if (_choiceIndex != _valueCount.Count - 1)
				throw new NondeterminismException();

			// Reset the choice counter as each path starts from the beginning
			_choiceIndex = -1;

			// If this is the first path of the state, we definitely have to enumerate it
			if (_firstPath)
			{
				_firstPath = false;
				return true;
			}

			// Let's go through the entire stack to determine what we have to do next
			while (_chosenValues.Count > 0)
			{
				// Remove the value we've chosen last -- we've already chosen it, so we're done with it
				var chosenValue = _chosenValues.Remove();

				// If we have at least one other value to choose, let's do that next
				if (_valueCount.Peek() > chosenValue + 1)
				{
					_chosenValues.Push(chosenValue + 1);
					return true;
				}

				// Otherwise, we've chosen all values of the last choice, so we're done with it
				_valueCount.Remove();
			}

			// If we reach this point, we know that we've chosen all values of all choices, so there are no further paths
			return false;
		}

		/// <summary>
		///   Handles a nondeterministic choice that chooses between <paramref name="valueCount" /> values.
		/// </summary>
		/// <param name="valueCount">The number of values that can be chosen.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int HandleChoice(int valueCount)
		{
			++_choiceIndex;

			// If we have a preselected value that we should choose for the current path, return it
			if (_choiceIndex < _chosenValues.Count)
				return _chosenValues[_choiceIndex];

			// We haven't encountered this choice before; store the value count and return the first value
			_valueCount.Push(valueCount);
			_chosenValues.Push(0);

			return 0;
		}

		/// <summary>
		///   Undoes the choice identified by the <paramref name="choiceIndex" />.
		/// </summary>
		/// <param name="choiceIndex">The index of the choice that should be undone.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void Undo(int choiceIndex)
		{
			// We disable a choice by setting the number of values that we have yet to choose to 0, effectively
			// turning the choice into a deterministic selection of the value at index 0
			_valueCount[choiceIndex] = 0;
		}

		/// <summary>
		///   Sets the choices that should be made during the next step.
		/// </summary>
		/// <param name="choices">The choices that should be made.</param>
		internal void SetChoices(int[] choices)
		{
			Requires.NotNull(choices, nameof(choices));

			foreach (var choice in choices)
			{
				_chosenValues.Push(choice);
				_valueCount.Push(0);
			}
		}

		/// <summary>
		///   Clears all choice information.
		/// </summary>
		internal void Clear()
		{
			_chosenValues.Clear();
			_valueCount.Clear();
			_choiceIndex = -1;
		}

		/// <summary>
		///   Gets the choices that were made to generate the last transitions.
		/// </summary>
		internal IEnumerable<int> GetChoices()
		{
			for (var i = 0; i < _chosenValues.Count; ++i)
				yield return _chosenValues[i];
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_chosenValues.SafeDispose();
			_valueCount.SafeDispose();
		}
	}
}