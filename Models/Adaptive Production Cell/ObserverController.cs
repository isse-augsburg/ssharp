using System.Collections.Generic;
using SafetySharp.Modeling;

namespace ProductionCell
{
    abstract class ObserverController : Component
    {
		[Hidden]
	    public List<Agent> Agents;

		[Hidden]
	    public Task CurrentTask;

		[Hidden(HideElements=true)]
        protected ObjectPool<OdpRole> RolePool = new ObjectPool<OdpRole>(10);

	    private bool _reconfed;

		public bool Unsatisfiable { get; protected set; }

        public abstract void Reconfigure();
        public override void Update()
        {
	        if (_reconfed)
		        return;

            base.Update();
            Reconfigure();

	        _reconfed = true;
        }

        protected ObserverController()
        {
            for (int i = 0; i < 10; i++)
            {
                RolePool.Add(new OdpRole());
            }
        }
    }
}