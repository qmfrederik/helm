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
        private static readonly IPEndPoint tillerEndpoint = new IPEndPoint(IPAddress.Parse("172.17.0.3"), 44134);

        [Fact]
        public void ConstructorNullTest()
        {
            Assert.Throws<ArgumentNullException>(() => new TillerClient((string)null));
        }
    }
}
