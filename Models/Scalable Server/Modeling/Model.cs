using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.ScalableServer.Modeling
{
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	public class Model : ModelBase
	{
		[Root(Role.System)]
		public IBackend Backend;

		[Root(Role.System)]
		public IUserModel UserModel;

		public Model(IBackend backend,IUserModel userModel)
		{
			Backend = backend;
			UserModel = userModel;

			Bind(nameof(UserModel.Request), nameof(Backend.Request));
		}
	}
}
