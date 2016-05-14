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

namespace SafetySharp.CaseStudies.ProductionCell.Modeling.Plants
{
	using System.Collections.Generic;
	using System.Linq;
	using Controllers;
	using SafetySharp.Modeling;

	internal class Cart : Component
	{
		[Hidden(HideElements = true)]
		private readonly Route[] _routes;

		[Hidden]
		private string _name;

		private Robot _position;

		public Fault Broken = new PermanentFault();

		public Workpiece LoadedWorkpiece;

		public Cart(Robot startPosition, params Route[] routes)
		{
			_routes = routes;
			_position = startPosition;
		}

		public Cart()
		{
		}

		public bool HasWorkpiece => LoadedWorkpiece != null;

		public virtual bool MoveTo(Robot robot)
		{
			if (_position == robot)
				return true;

			var route = _routes.SingleOrDefault(r => r.CanNavigate(_position, robot));

			if (route == null)
				return false;

			_position = route.To;
			return true;
		}

		public void SetNames(int cartId)
		{
			_name = $"C{cartId}";
			Broken.Name = $"C{cartId}.Broken";

			foreach (var route in _routes)
				route.Blocked.Name = $"C{cartId}.{route.From.Name}->{route.To.Name}.Blocked";
		}

		public override string ToString()
		{
			return $"{_name}@{_position.Name}: Workpiece: {LoadedWorkpiece?.Name}";
		}

		[FaultEffect(Fault = nameof(Broken))]
		internal class BrokenEffect : Cart
		{
			public override bool MoveTo(Robot robot) => false;
			public override bool CanMove(Robot robot) => false;
		}

		public virtual bool CanMove(Robot robot)
		{
			if (_position == robot)
				return true;

			return _routes.Any(r => r.CanNavigate(_position, robot));
		}
	}
}