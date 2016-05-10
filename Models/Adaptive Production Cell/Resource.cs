using System.Collections.Generic;
using SafetySharp.Modeling;

namespace ProductionCell
{
    class Resource : Component
    {
        List<Capability> _state;
        Task _task;
    }
}