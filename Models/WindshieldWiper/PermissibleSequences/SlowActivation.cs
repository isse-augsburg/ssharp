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

namespace Wiper.PermissibleSequences
{
	using DataStructures;
	using Model;
	using SafetySharp.Modeling;

	public enum SlowActivationState
	{
		WaitForRequest,
		WaitForSetSpeed,
		WaitForSetActive,
		Failed
	}

	public class SlowActivation : WiperPermissibleSequence
	{
		StateMachine<SlowActivationState> ValidSequenceVerifier = new StateMachine<SlowActivationState>(SlowActivationState.WaitForRequest);

		private bool IsMessageOfInterest(Message message)
		{
			return true;
		}

		public void Check(CurrentState state, Message message)
		{
			ValidSequenceVerifier
				.Transition(
					from: SlowActivationState.WaitForRequest,
					to: SlowActivationState.WaitForSetSpeed,
					guard: IsMessageFromUserToWiper(message) &&
						   IsVehicleRunning(state) &&
						   IsWiperInstalled(state) &&
						   WiperControllerInErrorState(state))
				.Transition(
					from: SlowActivationState.WaitForSetSpeed,
					to: SlowActivationState.WaitForSetActive,
					guard: IsMessageOfInterest(message) &&
						   (IsMessageFromWiperToActuator(message) &&
							message.Method == nameof(WiperEcu.SetWiperSpeed) &&
							message.Parameters.Equals(new[] { WiperSpeed.Slow })))
				.Transition(
					from: SlowActivationState.WaitForSetSpeed,
					to: SlowActivationState.Failed,
					guard: IsMessageOfInterest(message) &&
						   !(IsMessageFromWiperToActuator(message) &&
							 message.Method == nameof(WiperEcu.SetWiperSpeed) &&
							 message.Parameters.Equals(new[] { WiperSpeed.Slow })))
				.Transition(
					from: SlowActivationState.WaitForSetActive,
					to: SlowActivationState.WaitForRequest,
					guard: IsMessageOfInterest(message) &&
						   (IsMessageFromWiperToActuator(message) &&
							message.Method == nameof(WiperEcu.WiperState) &&
							message.Parameters.Equals(new[] { WiperState.Active })))
				.Transition(
					from: SlowActivationState.WaitForSetActive,
					to: SlowActivationState.Failed,
					guard: IsMessageOfInterest(message) &&
						   !(IsMessageFromWiperToActuator(message) &&
							 message.Method == nameof(WiperEcu.WiperState) &&
							 message.Parameters.Equals(new[] { WiperState.Active })));
		}

		public override bool IsFailed()
		{
			return ValidSequenceVerifier.State == SlowActivationState.Failed;
		}
	}



}
