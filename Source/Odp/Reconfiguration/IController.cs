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

namespace SafetySharp.Odp.Reconfiguration
{
	using System;
	using System.Threading.Tasks;
	using JetBrains.Annotations;

	/// <summary>
	///  Calculates configurations for a given <see cref="ITask"/>.
	/// </summary>
    public interface IController
    {
		/// <summary>
		///  The set of agents the controller knows about and controls.
		/// </summary>
		BaseAgent[] Agents { get; }

		/// <summary>
		///  Asynchronously calculates a <see cref="ConfigurationUpdate"/>.
		/// </summary>
		/// <param name="context">An arbitrary context object some controllers may assign meaning to, while others may not.</param>
		/// <param name="task">The task for which configurations should be updated.</param>
		[NotNull, ItemNotNull] Task<ConfigurationUpdate> CalculateConfigurationsAsync(object context, ITask task);

		/// <summary>
		///  Raised when the controller has calculated configurations, just before <see cref="CalculateConfigurationsAsync"/> returns.
		/// </summary>
		event Action<ITask, ConfigurationUpdate> ConfigurationsCalculated;
	}
}
