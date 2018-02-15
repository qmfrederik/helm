using Helm.Charts;
using Helm.Helm;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Helm.IntegrationTests
{
    public class TillerClientTests
    {
        [Fact]
        public async Task GetVersionStreamTest()
        {
            using (var socket = await TestConfiguration.GetSocket())
            using (NetworkStream stream = new NetworkStream(socket))
            {
                var client = new TillerClient(stream);
                var version = await client.GetVersionAsync();
                Assert.NotNull(version);
            }
        }

        [Fact]
        public async Task InstallReleaseTest()
        {
            using (var socket = await TestConfiguration.GetSocket())
            using (NetworkStream tillerStream = new NetworkStream(socket))
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = Chart.Open(chartStream);
                var client = new TillerClient(tillerStream);

                var result = await client.InstallReleaseAsync(chart, string.Empty, "hello-world", true).ConfigureAwait(false);
            }
        }
    }
}
