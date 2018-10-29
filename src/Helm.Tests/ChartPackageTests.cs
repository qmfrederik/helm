using Helm.Charts;
using System.IO;
using Xunit;
using System.Linq;

namespace Helm.Tests
{
    public class ChartPackageTests
    {
        [Fact]
        public static void OpenTest()
        {
            using (Stream stream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(stream);
                Assert.NotNull(chart);
            }
        }

        [Fact]
        public static void MetadataTest()
        {
            using (Stream stream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(stream);
                var metadata = chart.Metadata;

                Assert.NotNull(metadata);
                Assert.Equal("v1", metadata.ApiVersion);
                Assert.Equal("A Helm chart for Kubernetes", metadata.Description);
                Assert.Equal("hello-world", metadata.Name);
                Assert.Equal("0.1.0", metadata.Version);
            }
        }

        [Fact]
        public static void DependencyTest()
        {
            using (Stream stream = File.OpenRead("charts/kafka-0.8.8.tgz"))
            {
                var package = ChartPackage.Open(stream);
                var chart = package.Serialize();

                Assert.NotNull(chart.Metadata);
                Assert.NotNull(chart.Values);
                Assert.NotEmpty(chart.Templates);
                Assert.NotEmpty(chart.Dependencies);
                var dependChart = chart.Dependencies[0];

                Assert.NotNull(dependChart);
                Assert.NotNull(dependChart.Metadata);
                Assert.Equal("zookeeper", dependChart.Metadata.Name);
                Assert.NotNull(dependChart.Values);
                Assert.NotEmpty(dependChart.Templates);
                Assert.NotNull(dependChart.Values.Raw);
            }
        }
    }
}
