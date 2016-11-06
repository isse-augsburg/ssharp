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

namespace SafetySharp.CaseStudies.RobotCell.Analysis
{
	using System.Linq;
	using Modeling;
	using Modeling.Controllers;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	public class ModelCheckingTests
	{
		[Test]
		public void EnumerateStateSpace()
		{
			var model = SampleModels.DefaultInstance<FastController>();
			model.Faults.SuppressActivations();

			var modelChecker = new SSharpChecker { Configuration = { StateCapacity = 1 << 10 } };
			var result = modelChecker.CheckInvariant(model, true);

			Assert.IsTrue(result.FormulaHolds);
		}

		[Test]
		public void NoDamagedWorkpieces()
		{
			var model = SampleModels.DefaultInstance<FastController>();
			model.Faults.SuppressActivations();

			var modelChecker = new SSharpChecker { Configuration = { StateCapacity = 1 << 10 } };
			var result = modelChecker.CheckInvariant(model, !model.Workpieces.Any(w => w.IsDamaged));

			Assert.IsTrue(result.FormulaHolds);
		}

		[Test]
		public void AllWorkpiecesCompleteEventually()
		{
			var model = SampleModels.DefaultInstance<FastController>();
			model.Faults.SuppressActivations();

			var modelChecker = new SSharpChecker { Configuration = { StateCapacity = 1 << 10 } };
			var result = modelChecker.CheckInvariant(model, !model.Workpieces.All(w => w.IsComplete));

			Assert.IsFalse(result.FormulaHolds);
		}

		[Test]
		public void HasResourceAndHasWorkpieceMatch()
		{
			var model = SampleModels.DefaultInstance<FastController>();
			model.Faults.SuppressActivations();

			var modelChecker = new SSharpChecker { Configuration = { StateCapacity = 1 << 10 } };
			var result = modelChecker.CheckInvariant(model,
				model.RobotAgents.All(a => a.HasResource == a.Robot.HasWorkpiece) &&
				model.CartAgents.All(a => a.HasResource == a.Cart.HasWorkpiece));

			Assert.IsTrue(result.FormulaHolds);
		}
	}
}