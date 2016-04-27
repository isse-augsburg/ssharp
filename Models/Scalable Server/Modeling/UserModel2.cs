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

namespace SafetySharp.CaseStudies.ScalableServer.Modeling
{
	using SafetySharp.Modeling;
	
	public class UserModel2 : Component
	{
		public enum ChangeOfRegularRequests
		{
			LessRequests,
			EqualRequests,
			MoreRequests,
		}

		public UserModel2(IBackend backend)
		{
			_backend = backend;
		}

		private readonly IBackend _backend;

		public Reward UserReward;
		
		public QualitativeAmount RegularRequests = QualitativeAmount.None;


		public readonly Probability PVeryRare = new Probability(0.000001);
		public readonly Probability PAlmostAlways = new Probability(0.999999);
		public readonly Probability P98 = new Probability(98);
		public readonly Probability P1 = new Probability(0.01);
		public readonly Probability P5 = new Probability(0.05);
		public readonly Probability P10 = new Probability(0.1);
		public readonly Probability P20 = new Probability(0.2);
		public readonly Probability P30 = new Probability(0.3);
		public readonly Probability P40 = new Probability(0.4);
		public readonly Probability P50 = new Probability(0.5);
		public readonly Probability P60 = new Probability(0.6);
		public readonly Probability P70 = new Probability(0.7);
		public readonly Probability P80 = new Probability(0.8);
		public readonly Probability P90 = new Probability(0.9);

		public override void Update()
		{
			// here, the slashdot effect is modeled
			var slashDotEffectOccurs = Choose(new Option<bool>(PVeryRare, true),
											  new Option<bool>(PAlmostAlways, false));
			ChangeOfRegularRequests changeOfRegularRequests;
			QualitativeAmount actualRequests;
			if (slashDotEffectOccurs)
			{
				changeOfRegularRequests = ChangeOfRegularRequests.MoreRequests;
				actualRequests = QualitativeAmount.Many;
			}
			else
			{

				// here, the normal change of standard users is modeled
				changeOfRegularRequests = Choose(new Option<ChangeOfRegularRequests>(P1, ChangeOfRegularRequests.LessRequests),
										   new Option<ChangeOfRegularRequests>(P98, ChangeOfRegularRequests.EqualRequests),
										   new Option<ChangeOfRegularRequests>(P1, ChangeOfRegularRequests.MoreRequests));

				switch (changeOfRegularRequests)
				{
					case ChangeOfRegularRequests.LessRequests:
						actualRequests = RegularRequests.Less();
						break;
					case ChangeOfRegularRequests.EqualRequests:
						actualRequests = RegularRequests;
						break;
					case ChangeOfRegularRequests.MoreRequests:
						actualRequests = RegularRequests.More();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			var result = _backend.Request(actualRequests);

			switch (result)
			{
				case RequestResult.Complete:
					UserReward.Positive(3 * actualRequests.Value());
					// In standard mode when everything works the change of requests has a direct impact
					// on the number of RegularReaders
					switch (changeOfRegularRequests)
					{
						case ChangeOfRegularRequests.LessRequests:
							RegularRequests = RegularRequests.Less();
							break;
						case ChangeOfRegularRequests.EqualRequests:
							break;
						case ChangeOfRegularRequests.MoreRequests:
							RegularRequests = RegularRequests.More();
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					break;
				case RequestResult.Degraded:
					UserReward.Positive(actualRequests.Value());
					// In degraded mode when computing intensive tasks are switched of no new RegularReaders
					// can be acquired but previous some RegularReaders could have been lost
					switch (changeOfRegularRequests)
					{
						case ChangeOfRegularRequests.LessRequests:
							RegularRequests = RegularRequests.Less();
							break;
						case ChangeOfRegularRequests.EqualRequests:
						case ChangeOfRegularRequests.MoreRequests:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					break;
				case RequestResult.Failed:
					// When the backend is not reachable some RegularReaders are lost.
					UserReward.Negative(actualRequests.Value());
					RegularRequests = RegularRequests.Less();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
