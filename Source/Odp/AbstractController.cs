﻿// The MIT License (MIT)
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

namespace SafetySharp.Odp
{
	using System.Collections.Generic;
	using Modeling;

	public abstract class AbstractController : Component, IController
	{
		[Hidden(HideElements = true)]
		public BaseAgent[] Agents { get; }

		public AbstractController(BaseAgent[] agents)
		{
			Agents = agents;
		}

		public virtual bool ReconfigurationFailure
		{
			get;
			protected set;
		}

		public abstract Dictionary<BaseAgent, IEnumerable<Role>> CalculateConfigurations(params ITask[] tasks);

		protected Role GetRole(ITask recipe, BaseAgent input, Condition? previous)
		{
			var role = new Role();

			// update precondition
			role.PreCondition.Task = recipe;
			role.PreCondition.Port = input;
			role.PreCondition.ResetState();
			if (previous != null)
				role.PreCondition.CopyStateFrom(previous.Value);

			// update postcondition
			role.PostCondition.Task = recipe;
			role.PostCondition.Port = null;
			role.PostCondition.ResetState();
			role.PostCondition.CopyStateFrom(role.PreCondition);

			role.Clear();

			return role;
		}
	}
}