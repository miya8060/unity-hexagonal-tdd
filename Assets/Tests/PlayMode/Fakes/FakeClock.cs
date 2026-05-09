using UnityHexagonalTdd.Application;

namespace UnityHexagonalTdd.Tests.PlayMode.Fakes
{
    public sealed class FakeClock : IClock
    {
        public float Now { get; set; }

        public FakeClock(float initial = 0f)
        {
            Now = initial;
        }
    }
}
