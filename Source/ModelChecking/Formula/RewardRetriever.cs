using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.Runtime.Serialization
{
	using Modeling;
	
	public class RewardRetriever
	{
		public string Label { get; }

		public Func<Reward> Retriever { get; }
		
		public RewardRetriever(string label, Func<Reward> retriever)
		{
			Label = label ?? "Reward" + Guid.NewGuid().ToString().Replace("-", String.Empty);
			Retriever = retriever;
		}

		public static implicit operator RewardRetriever(Func<Reward> retriever)
		{
			return new RewardRetriever(null,retriever);
		}
	}
}
