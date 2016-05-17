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
		[Root(RootKind.Plant)]
		public IBackend Backend;

		[Root(RootKind.Plant)]
		public IUserModel UserModel;

		public Model(IBackend backend,IUserModel userModel)
		{
			Backend = backend;
			UserModel = userModel;

			Bind(nameof(UserModel.Request), nameof(Backend.Request));
		}
	}
}
