using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wiper.Model;

namespace Wiper.Scenarios
{
	using DataStructures;
	using SafetySharp.Modeling;

	public class IndeterministicInput : WiperScenario
	{
		//private int CurrentStep = 0;

		public IndeterministicInput()
		{
		}

		public override extern void WiperControlStalkSendRequest(WiperRequest request);

		public override void Update()
		{
			var request = Choose(WiperRequest.Fast, WiperRequest.Slow, WiperRequest.Increase, WiperRequest.Off);
			WiperControlStalkSendRequest(request);
			/*
			switch (CurrentStep)
			{
				case 0:
					break;
				case 0:
					break;
				case 0:
					break;
			}
			*/
		}
	}
}
