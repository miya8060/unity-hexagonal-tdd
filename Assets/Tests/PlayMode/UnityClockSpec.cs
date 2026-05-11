using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityHexagonalTdd.Presentation.Adapters;

namespace UnityHexagonalTdd.Tests.PlayMode
{
    public sealed class UnityClockSpec
    {
        [UnityTest]
        public IEnumerator Now_advances_with_Time_time()
        {
            var clock = new UnityClock();
            var t0 = clock.Now;
            yield return new WaitForSeconds(0.1f);
            Assert.That(clock.Now, Is.GreaterThan(t0));
        }
    }
}
