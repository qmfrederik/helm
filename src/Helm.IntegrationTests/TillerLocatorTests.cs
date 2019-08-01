using Helm.Helm;
using k8s;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Helm.IntegrationTests
{
    public class TillerLocatorTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TillerLocatorTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task LocateTest()
        {
            _testOutputHelper.WriteLine("Trying to locate Tiller endpoint");

            Kubernetes kubernetes = new Kubernetes(TestConfiguration.Configure());

            TillerLocator locator = new TillerLocator(kubernetes);
            var endPoint = await locator.Locate().ConfigureAwait(false);
            
            Assert.NotNull(endPoint);

            _testOutputHelper.WriteLine($"Tiller endpoint located at {endPoint}");
        }
    }
}
