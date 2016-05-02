using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ScalableServer.Modeling
{
	using SafetySharp.Modeling;

	public interface IUserModel : IComponent
	{
		[Required]
		RequestResult Request(QualitativeAmount requestNumber);

		[Provided]
		Reward GetReward();
	}
}
