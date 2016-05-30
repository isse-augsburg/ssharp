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

namespace SafetySharp.CaseStudies.CircuitBasedPressureTank.Modeling
{
	using System.Diagnostics;
	using HemodialysisMachine.Utilities.BidirectionalFlow;
	using SafetySharp.Modeling;

	public class Circuits : Component
	{
		private readonly CurrentCombinator _currentCombinatorControlCircuit;
		private readonly CurrentCombinator _currentCombinatorMotorCircuit;

		public readonly PressureSensor Sensor;
		public readonly TimerRelay Timer;
		public readonly Relay K1;
		public readonly Relay K2;
		public readonly Switch Switch;
		public readonly Pump Pump;
		public readonly PowerSource PowerSourceControlCircuit;
		public readonly PowerSource PowerSourceMotorCircuit;
		public readonly CurrentSplitter FirstSplitOfControlCircuit;
		public readonly CurrentMerger FirstMergeOfControlCircuit;
		public readonly CurrentSplitter SecondSplitOfControlCircuit;
		public readonly CurrentMerger SecondMergeOfControlCircuit;


		public Circuits()
		{
			_currentCombinatorControlCircuit = new CurrentCombinator();
			_currentCombinatorMotorCircuit = new CurrentCombinator();

			Sensor = new PressureSensor();
			Timer = new TimerRelay(openOnTimeout:true);
			K1 = new Relay(openOnPower:false,initiallyClosed:false);
			K2 = new Relay(openOnPower: false, initiallyClosed: false);
			Switch  = new Switch();
			Pump = new Pump();
			PowerSourceControlCircuit = new PowerSource();
			PowerSourceMotorCircuit = new PowerSource(); ;
			FirstSplitOfControlCircuit = new CurrentSplitter(2);
			FirstMergeOfControlCircuit = new CurrentMerger(2);
			SecondSplitOfControlCircuit = new CurrentSplitter(2);
			SecondMergeOfControlCircuit = new CurrentMerger(2);


			//Connections of the Control Circuit
			_currentCombinatorControlCircuit.ConnectOutWithIn(PowerSourceControlCircuit.PositivePole,
				FirstSplitOfControlCircuit);
			_currentCombinatorControlCircuit.ConnectOutWithIn(FirstSplitOfControlCircuit, 0,
				Timer.LoadCircuit); 
			_currentCombinatorControlCircuit.ConnectOutWithIn(Timer.LoadCircuit,
				K1.ControlCircuit);
			_currentCombinatorControlCircuit.ConnectOutWithIn(K1.ControlCircuit,
				FirstMergeOfControlCircuit,0);
			_currentCombinatorControlCircuit.ConnectOutWithIn(FirstSplitOfControlCircuit, 1,
				Sensor.MainCircuit);
			_currentCombinatorControlCircuit.ConnectOutWithIn(Sensor.MainCircuit,
				Timer.ControlCircuit);
			_currentCombinatorControlCircuit.ConnectOutWithIn(Timer.ControlCircuit,
				K2.ControlCircuit);
			_currentCombinatorControlCircuit.ConnectOutWithIn(K2.ControlCircuit,
				FirstMergeOfControlCircuit, 1);
			_currentCombinatorControlCircuit.ConnectOutWithIn(FirstMergeOfControlCircuit,
				SecondSplitOfControlCircuit);
			_currentCombinatorControlCircuit.ConnectOutWithIn(SecondSplitOfControlCircuit,0,
				K1.LoadCircuit);
			_currentCombinatorControlCircuit.ConnectOutWithIn(K1.LoadCircuit,
				SecondMergeOfControlCircuit,0);
			_currentCombinatorControlCircuit.ConnectOutWithIn(SecondSplitOfControlCircuit, 1,
				Switch.MainCircuit);
			_currentCombinatorControlCircuit.ConnectOutWithIn(Switch.MainCircuit,
				SecondMergeOfControlCircuit, 1);
			_currentCombinatorControlCircuit.ConnectOutWithIn(SecondMergeOfControlCircuit,
				PowerSourceControlCircuit.NegativePole);
			_currentCombinatorControlCircuit.CommitFlow();

			//Connections of the Motor Circuit
			_currentCombinatorMotorCircuit.ConnectOutWithIn(PowerSourceMotorCircuit.PositivePole,
				K2.LoadCircuit);
			_currentCombinatorMotorCircuit.ConnectOutWithIn(K2.LoadCircuit,
				Pump.MainCircuit);
			_currentCombinatorMotorCircuit.ConnectOutWithIn(Pump.MainCircuit,
				PowerSourceMotorCircuit.NegativePole);
			_currentCombinatorMotorCircuit.CommitFlow();


			K1.StuckFault.Name = $"{nameof(K1)}Stuck";
			K2.StuckFault.Name = $"{nameof(K2)}Stuck";
			Timer.StuckFault.Name = $"{nameof(Timer)}Stuck";
		}

		public override void Update()
		{
			//Debugger.Break();
			_currentCombinatorControlCircuit.Update();
			_currentCombinatorMotorCircuit.Update();

			Update(Sensor,Timer, K1, K2,Switch, PowerSourceControlCircuit);

			Update(Pump, PowerSourceMotorCircuit);
		}
	}
}
