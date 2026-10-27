using System;
using NUnit.Framework;

namespace Nyonyo.Framework.Editor.Tests
{
    public class ServicesTests
    {
        private interface ITestService { }

        private class TestService : ITestService { }

        [TearDown]
        public void TearDown() => Services.SetRegistry(null);

        [Test]
        public void Get_WithoutRegistry_Throws()
        {
            Services.SetRegistry(null);

            Assert.Throws<InvalidOperationException>(() => Services.Get<ITestService>());
        }

        [Test]
        public void TryGet_WithoutRegistry_ReturnsFalse()
        {
            Services.SetRegistry(null);

            Assert.IsFalse(Services.TryGet(out ITestService service));
            Assert.IsNull(service);
        }

        [Test]
        public void Get_ReturnsServiceFromRegistry()
        {
            var registry = new ServiceRegistry();
            var service = new TestService();
            registry.Register<ITestService>(service);
            Services.SetRegistry(registry);

            Assert.AreSame(service, Services.Get<ITestService>());
        }
    }
}
