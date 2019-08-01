using Grpc.Core;
using Helm.Charts;
using Helm.Helm;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Helm.IntegrationTests
{
    public class TillerClientTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TillerClientTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task GetHistoryTest()
        {
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(chartStream);

                _testOutputHelper.WriteLine("Trying to get history");

                var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());
                await Assert.ThrowsAsync<RpcException>(() => client.GetHistory(nameof(GetHistoryTest).ToLower(), 1));

                _testOutputHelper.WriteLine("Trying to install release");

                await client.InstallRelease(chart.Serialize(), string.Empty, nameof(GetHistoryTest).ToLower(), true, wait: true);

                await Task.Delay(100);
                _testOutputHelper.WriteLine("Trying to get history");

                var history = await client.GetHistory(nameof(GetHistoryTest).ToLower(), 1);
                Assert.Single(history);

                _testOutputHelper.WriteLine("Trying to uninstall release");

                var response = await client.UninstallRelease(nameof(GetHistoryTest).ToLower(), purge: false);
                Assert.Empty(response.Info);
                Assert.NotNull(response.Release);

                _testOutputHelper.WriteLine("Trying to purge uninstall release");

                response = await client.UninstallRelease(nameof(GetHistoryTest).ToLower(), purge: true);
                Assert.Empty(response.Info);
                Assert.NotNull(response.Release);
            }
        }

        [Fact]
        public async Task GetReleaseContentTest()
        {
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(chartStream);

                _testOutputHelper.WriteLine("Trying to get release content");

                var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());
                await Assert.ThrowsAsync<RpcException>(() => client.GetReleaseContent(nameof(GetReleaseContentTest).ToLower(), 0));

                _testOutputHelper.WriteLine("Trying to install release");

                await client.InstallRelease(chart.Serialize(), string.Empty, nameof(GetReleaseContentTest).ToLower(), true, wait: true);
                await Task.Delay(100);

                _testOutputHelper.WriteLine("Trying to get release content");

                var response = await client.GetReleaseContent(nameof(GetReleaseContentTest).ToLower(), 0);
                Assert.NotNull(response);

                _testOutputHelper.WriteLine("Trying to uninstall release");

                await client.UninstallRelease(nameof(GetReleaseContentTest).ToLower(), purge: true);
            }
        }

        [Fact]
        public async Task GetReleaseStatusTest()
        {
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(chartStream);

                _testOutputHelper.WriteLine("Trying to get release status");

                var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());
                await Assert.ThrowsAsync<RpcException>(() => client.GetReleaseStatus(nameof(GetReleaseStatusTest).ToLower(), 0));

                _testOutputHelper.WriteLine("Trying to install release");

                await client.InstallRelease(chart.Serialize(), string.Empty, nameof(GetReleaseStatusTest).ToLower(), true, wait: true);
                var response = await client.GetReleaseStatus(nameof(GetReleaseStatusTest).ToLower(), 0);
                Assert.NotNull(response);

                _testOutputHelper.WriteLine("Trying to uninstall release");

                await client.UninstallRelease(nameof(GetReleaseStatusTest).ToLower(), purge: true);
            }
        }

        [Fact]
        public async Task GetVersionStreamTest()
        {
            _testOutputHelper.WriteLine("Trying to get version");

            var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());
            var version = await client.GetVersion();
            Assert.NotNull(version);
        }

        [Fact]
        public async Task InstallReleaseTest()
        {
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(chartStream);
                var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());

                _testOutputHelper.WriteLine("Trying to install release");

                var result = await client.InstallRelease(chart.Serialize(), string.Empty, nameof(InstallReleaseTest).ToLower(), true).ConfigureAwait(false);
                Assert.NotNull(result);
                
                _testOutputHelper.WriteLine("Trying to uninstall release");

                await client.UninstallRelease(nameof(InstallReleaseTest).ToLower(), purge: true);
            }
        }

        [Fact]
        public async Task ListReleasesTest()
        {
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(chartStream);
                var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());

                _testOutputHelper.WriteLine("Trying to list releases");

                var releases = await client.ListReleases(nameof(ListReleasesTest).ToLower(), limit: 0, @namespace: "default").ConfigureAwait(false);
                Assert.Empty(releases);

                _testOutputHelper.WriteLine("Trying to install release");

                var result = await client.InstallRelease(chart.Serialize(), string.Empty, nameof(ListReleasesTest).ToLower(), true).ConfigureAwait(false);
                releases = await client.ListReleases(nameof(ListReleasesTest).ToLower(), limit: 0, @namespace: "default").ConfigureAwait(false);
                var release = Assert.Single(releases);

                await client.UninstallRelease(nameof(ListReleasesTest).ToLower(), purge: true);
            }
        }

        [Fact]
        public async Task ListReleasesAcceptEmptyParameterTest()
        {
            _testOutputHelper.WriteLine("Trying to list releases");

            var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());
            var releases = await client.ListReleases().ConfigureAwait(false);
        }
    }
}
