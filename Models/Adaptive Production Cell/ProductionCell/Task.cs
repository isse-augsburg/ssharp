using System.Collections.Generic;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class Task : Component
    {
        public List<Capability> RequiresCapabilities { get; set; }

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