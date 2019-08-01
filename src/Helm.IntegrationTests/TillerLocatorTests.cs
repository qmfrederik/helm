using System;
using Helm.Helm;
using k8s;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Helm.IntegrationTests
{
    public class ConsoleOutputHelper : ITestOutputHelper
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }

    public class TillerLocatorTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TillerLocatorTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = new ConsoleOutputHelper();
        }

        [Fact]
        public async Task LocateTest()
        {
            _testOutputHelper.WriteLine("Trying to locate Tiller endpoint");

            Kubernetes kubernetes = new Kubernetes(TestConfiguration.Configure());
            var nodes = await kubernetes.ListNodeAsync();
            _testOutputHelper.WriteLine($"{nodes.Items.Count} found");

            TillerLocator locator = new TillerLocator(kubernetes);
            var endPoint = await locator.Locate().ConfigureAwait(false);
            
            Assert.NotNull(endPoint);

            _testOutputHelper.WriteLine($"Tiller endpoint located at {endPoint}");
        }
    }
}
