using System.Collections.Generic;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class Task : Component
    {
	    [Hidden(HideElements = true)]
	    public readonly List<Capability> RequiresCapabilities;

	    public Task(List<Capability> requiresCapabilities)
	    {
		    RequiresCapabilities = requiresCapabilities;
	    }

	    public string[] GetTaskAsStrings()
        {
            List<string> capaList = new List<string>(RequiresCapabilities.Count);
            foreach (var capa in RequiresCapabilities)
            {
                capaList.Add(capa.ToString());
            }
            return capaList.ToArray();
        }
    }
}