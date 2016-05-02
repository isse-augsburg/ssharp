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
	
	//Constant amounts of visits. Every user makes each time step one request
	public class UserModel1 : Component, IUserModel
	{
		public UserModel1(QualitativeAmount amountOfVisits)
		{
			RegularUsers = amountOfVisits;
		}

		public Reward UserReward;
		
		public readonly QualitativeAmount RegularUsers;
		

		public override void Update()
		{
			var result = Request(RegularUsers);

			switch (result)
			{
				case RequestResult.Complete:
					UserReward.Positive(3 * RegularUsers.Value());
					break;
				case RequestResult.Degraded:
					UserReward.Positive(RegularUsers.Value());
					break;
				case RequestResult.Failed:
					UserReward.Negative(RegularUsers.Value());
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		[Required]
		public extern RequestResult Request(QualitativeAmount requestNumber);

		[Provided]
		public Reward GetReward()
		{
			return UserReward;
		}
	}
}
