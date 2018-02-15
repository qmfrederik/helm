using Helm.Charts;
using System.IO;
using Xunit;

namespace Helm.Tests
{
    public class ChartTests
    {
        [Fact]
        public static void OpenTest()
        {
            using (Stream stream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = Chart.Open(stream);
                Assert.NotNull(chart);
            }
        }

        [Fact]
        public static void MetadataTest()
        {
            using (Stream stream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = Chart.Open(stream);
                var metadata = chart.Metadata;

                Assert.NotNull(metadata);
                Assert.Equal("v1", metadata.ApiVersion);
                Assert.Equal("A Helm chart for Kubernetes", metadata.Description);
                Assert.Equal("hello-world", metadata.Name);
                Assert.Equal("0.1.0", metadata.Version);
            }
        }
    }
}
