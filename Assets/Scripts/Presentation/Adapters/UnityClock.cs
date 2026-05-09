using UnityEngine;
using UnityHexagonalTdd.Application;

namespace UnityHexagonalTdd.Presentation.Adapters
{
    public sealed class UnityClock : IClock
    {
        public float Now => Time.time;
    }
}
