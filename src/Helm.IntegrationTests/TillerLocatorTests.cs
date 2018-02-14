using Helm.Helm;
using k8s;
using System.Threading.Tasks;
using Xunit;

namespace Helm.IntegrationTests
{
    public class TillerLocatorTests
    {
        [Fact]
        public async Task LocateTest()
        {
            Kubernetes kubernetes = new Kubernetes(TestConfiguration.Configure());

            TillerLocator locator = new TillerLocator(kubernetes);
            var endPoint = await locator.Locate().ConfigureAwait(false);
            Assert.NotNull(endPoint);
        }
    }
}
