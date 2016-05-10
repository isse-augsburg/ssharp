using System.Collections.Generic;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class Condition : Component
    {
        Agent _port;
        Task _task;
        List<Capability> _state;
    }
}