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
            }
        }
    }
}
