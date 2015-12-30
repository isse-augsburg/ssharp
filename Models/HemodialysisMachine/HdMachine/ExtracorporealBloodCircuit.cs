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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine
{
	using Utilities;

	class ExtracorporealBloodCircuit
	{
		class ArterialBloodPump
		{
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		class ArteriaPressureTransducer
		{
			public BloodFlowSink BloodFlow = new BloodFlowSink();
		}

		class HeparinPump
		{
			public BloodFlowSource BloodFlow = new BloodFlowSource(() => new Blood());
		}

		class ArtierialChamber
		{
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		class VenousChamber
		{
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		class VenousPressureTransducer
		{
			public BloodFlowSink BloodFlow = new BloodFlowSink();
		}

		class VenousSafetyDetector
		{
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		class VenousTubingValve
		{
			public BloodFlowDirect BloodFlow = new BloodFlowDirect();
		}

		public BloodFlowComposite BloodFlow = new BloodFlowComposite();  

		private readonly ArterialBloodPump _arterialBloodPump = new ArterialBloodPump();
		private readonly ArteriaPressureTransducer _arteriaPressureTransducer = new ArteriaPressureTransducer();
		private readonly HeparinPump _heparinPump = new HeparinPump();
		private readonly ArtierialChamber _arterialChamber  = new ArtierialChamber();
		private readonly VenousChamber _venousChamber = new VenousChamber();
		private readonly VenousPressureTransducer _venousPressureTransducer = new VenousPressureTransducer();
		private readonly VenousSafetyDetector _venousSafetyDetector = new VenousSafetyDetector();
		private readonly VenousTubingValve _venousTubingValve = new VenousTubingValve();
		

		public ExtracorporealBloodCircuit(Dialyzer dialyzer, BloodFlowCombinator bloodFlowCombinator)
		{
			// The order of the connections matter
			bloodFlowCombinator.ConnectInWithIn(BloodFlow,
												new IFlowIn<Blood>[] {_arterialBloodPump.BloodFlow, _arteriaPressureTransducer.BloodFlow});
			bloodFlowCombinator.ConnectOutWithIn(new IFlowOut<Blood>[]{_arterialBloodPump.BloodFlow,_heparinPump.BloodFlow},
												 _arterialChamber.BloodFlow);
			bloodFlowCombinator.ConnectOutWithIn(_arterialChamber.BloodFlow,
												 dialyzer.BloodFlow);
			bloodFlowCombinator.ConnectOutWithIn(dialyzer.BloodFlow,
												 new IFlowIn<Blood>[] {_venousChamber.BloodFlow, _venousPressureTransducer.BloodFlow});
			bloodFlowCombinator.ConnectOutWithIn(_venousChamber.BloodFlow,
												 _venousSafetyDetector.BloodFlow);
			bloodFlowCombinator.ConnectOutWithIn(_venousSafetyDetector.BloodFlow,
												 _venousTubingValve.BloodFlow);
			bloodFlowCombinator.ConnectOutWithOut(_venousTubingValve.BloodFlow,
												  BloodFlow);
		}

	}
}
