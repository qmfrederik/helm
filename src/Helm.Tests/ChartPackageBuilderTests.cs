using Helm.Charts;
using SharpCompress.Archives.Tar;
using SharpCompress.Readers;
using System.IO;
using System.IO.Compression;
using Xunit;

namespace Helm.Tests
{
    public class ChartPackageBuilderTests
    {
        [Fact]
        public void RoundtripTest()
        {
            using (Stream stream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            using (MemoryStream packageStream = new MemoryStream())
            {
                var chart = ChartPackage.Open(stream);

                var builder = new ChartPackageBuilder();
                builder.Build(chart.Serialize(), packageStream);

                packageStream.Position = 0;

                using (GZipStream gzipStream = new GZipStream(packageStream, CompressionMode.Decompress, leaveOpen: true))
                using (MemoryStream decompressedStream = new MemoryStream())
                {
                    gzipStream.CopyTo(decompressedStream);
                    decompressedStream.Position = 0;

                    using (TarArchive archive = TarArchive.Open(decompressedStream, new ReaderOptions() { LeaveStreamOpen = true }))
                    {
                        Assert.Equal(8, archive.Entries.Count);
                        Assert.Contains(archive.Entries, e => e.Key == "hello-world/.helmignore");
                        Assert.Contains(archive.Entries, e => e.Key == "hello-world/Chart.yaml");
                        Assert.Contains(archive.Entries, e => e.Key == "hello-world/values.yaml");
                        Assert.Contains(archive.Entries, e => e.Key == "hello-world/templates/_helpers.tpl");
                        Assert.Contains(archive.Entries, e => e.Key == "hello-world/templates/deployment.yaml");
                        Assert.Contains(archive.Entries, e => e.Key == "hello-world/templates/ingress.yaml");
                        Assert.Contains(archive.Entries, e => e.Key == "hello-world/templates/NOTES.txt");
                        Assert.Contains(archive.Entries, e => e.Key == "hello-world/templates/service.yaml");

                        foreach(var entry in archive.Entries)
                        {
                            Assert.NotEqual(0, entry.Size);
                        }
                    }
                }
            }
        }
    }
}
