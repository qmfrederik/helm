using Helm.Helm;
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
            {
                using (NetworkStream stream = new NetworkStream(socket))
                {
                    TillerClient client = new TillerClient(stream);
                    var version = await client.GetVersionAsync();
                    Assert.NotNull(version);
                }
            }
        }
    }
}
