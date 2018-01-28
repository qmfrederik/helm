using Helm.Helm;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Helm.Tests
{
    public class TillerClientTests
    {
        [Fact]
        public void ConstructorNullTest()
        {
            Assert.Throws<ArgumentNullException>(() => new TillerClient((string)null));
        }

        [Fact(Skip = "Live test")]
        public async Task GetVersionTest()
        {
            TillerClient client = new TillerClient();
            var version = await client.GetVersionAsync();
            Assert.NotNull(version);
        }

        [Fact(Skip = "Live test")]
        public async Task GetVersionStreamTest()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 44134)).ConfigureAwait(false);

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
