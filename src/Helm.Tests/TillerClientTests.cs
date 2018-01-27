using Helm.Helm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Helm.Tests
{
    public class TillerClientTests
    {
        [Fact]
        public void ConstructorNullTest()
        {
            Assert.Throws<ArgumentNullException>(() => new TillerClient(null));
        }

        [Fact(Skip = "Live test")]
        public async Task GetVersionTest()
        {
            TillerClient client = new TillerClient();
            var version = await client.GetVersionAsync();
            Assert.NotNull(version);
        }
    }
}
