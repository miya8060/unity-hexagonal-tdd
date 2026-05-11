using NUnit.Framework;
using UnityHexagonalTdd.Domain;

namespace UnityHexagonalTdd.Tests.EditMode
{
    public class GreeterSpec
    {
        [Test]
        public void Greet_ReturnsHelloWithGivenName()
        {
            Assert.That(Greeter.Greet("World"), Is.EqualTo("Hello, World!"));
        }
    }
}
