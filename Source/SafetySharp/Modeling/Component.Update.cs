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

namespace SafetySharp.Modeling
{
	public abstract partial class Component
	{
		/// <summary>
		///   Updates the state of the <paramref name="component" />.
		/// </summary>
		/// <param name="component">The component that should be updated.</param>
		protected static void Update(IComponent component)
		{
			component.Update();
		}

		/// <summary>
		///   Updates the state of <paramref name="component1" />, then the state of <paramref name="component2" />.
		/// </summary>
		/// <param name="component1">The first component that should be updated.</param>
		/// <param name="component2">The second component that should be updated.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		protected static void Update(IComponent component1, IComponent component2)
		{
			component1.Update();
			component2.Update();
		}

		/// <summary>
		///   Updates the state of <paramref name="component1" />, then the state of <paramref name="component2" />,
		///   and then the state of <paramref name="component3" />.
		/// </summary>
		/// <param name="component1">The first component that should be updated.</param>
		/// <param name="component2">The second component that should be updated.</param>
		/// <param name="component3">The third component that should be updated.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		protected static void Update(IComponent component1, IComponent component2, IComponent component3)
		{
			component1.Update();
			component2.Update();
			component3.Update();
		}

		/// <summary>
		///   Updates the state of <paramref name="component1" />, then the state of <paramref name="component2" />, then the state of
		///   <paramref name="component3" />, and then the state of <paramref name="component4" />.
		/// </summary>
		/// <param name="component1">The first component that should be updated.</param>
		/// <param name="component2">The second component that should be updated.</param>
		/// <param name="component3">The third component that should be updated.</param>
		/// <param name="component4">The fourth component that should be updated.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		protected static void Update(IComponent component1, IComponent component2, IComponent component3, IComponent component4)
		{
			component1.Update();
			component2.Update();
			component3.Update();
			component4.Update();
		}

		/// <summary>
		///   Updates the state of <paramref name="component1" />, then the state of <paramref name="component2" />, then the state of
		///   <paramref name="component3" />, then the state of <paramref name="component4" />, and then the state of
		///   <paramref name="component5" />.
		/// </summary>
		/// <param name="component1">The first component that should be updated.</param>
		/// <param name="component2">The second component that should be updated.</param>
		/// <param name="component3">The third component that should be updated.</param>
		/// <param name="component4">The fourth component that should be updated.</param>
		/// <param name="component5">The fifth component that should be updated.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		protected static void Update(IComponent component1, IComponent component2, IComponent component3, IComponent component4,
									 IComponent component5)
		{
			component1.Update();
			component2.Update();
			component3.Update();
			component4.Update();
			component5.Update();
		}

		/// <summary>
		///   Updates the state of <paramref name="component1" />, then the state of <paramref name="component2" />, then the state of
		///   <paramref name="component3" />, then the state of <paramref name="component4" />, then the state of
		///   <paramref name="component5" />, and then the state of <paramref name="component6" />.
		/// </summary>
		/// <param name="component1">The first component that should be updated.</param>
		/// <param name="component2">The second component that should be updated.</param>
		/// <param name="component3">The third component that should be updated.</param>
		/// <param name="component4">The fourth component that should be updated.</param>
		/// <param name="component5">The fifth component that should be updated.</param>
		/// <param name="component6">The sixth component that should be updated.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		protected static void Update(IComponent component1, IComponent component2, IComponent component3, IComponent component4,
									 IComponent component5, IComponent component6)
		{
			component1.Update();
			component2.Update();
			component3.Update();
			component4.Update();
			component5.Update();
			component6.Update();
		}

		/// <summary>
		///   Updates the state of <paramref name="component1" />, then the state of <paramref name="component2" />, then the state of
		///   <paramref name="component3" />, then the state of <paramref name="component4" />, then the state of
		///   <paramref name="component5" />, then the state of <paramref name="component6" />, and then the state of
		///   <paramref name="component7" />.
		/// </summary>
		/// <param name="component1">The first component that should be updated.</param>
		/// <param name="component2">The second component that should be updated.</param>
		/// <param name="component3">The third component that should be updated.</param>
		/// <param name="component4">The fourth component that should be updated.</param>
		/// <param name="component5">The fifth component that should be updated.</param>
		/// <param name="component6">The sixth component that should be updated.</param>
		/// <param name="component7">The seventh component that should be updated.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		protected static void Update(IComponent component1, IComponent component2, IComponent component3, IComponent component4,
									 IComponent component5, IComponent component6, IComponent component7)
		{
			component1.Update();
			component2.Update();
			component3.Update();
			component4.Update();
			component5.Update();
			component6.Update();
			component7.Update();
		}

		/// <summary>
		///   Updates the state of the <paramref name="components" /> in the order the components are contained in the array.
		/// </summary>
		protected static void Update(params IComponent[] components)
		{
			foreach (var component in components)
				component.Update();
		}
	}
}