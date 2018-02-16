﻿using Grpc.Core;
using Helm.Charts;
using Helm.Helm;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace Helm.IntegrationTests
{
    public class TillerClientTests
    {
        [Fact]
        public async Task GetHistoryTest()
        {
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(chartStream);

                var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());
                await Assert.ThrowsAsync<RpcException>(() => client.GetHistory(nameof(GetHistoryTest).ToLower(), 1));

                await client.InstallReleaseAsync(chart.Serialize(), string.Empty, nameof(GetHistoryTest).ToLower(), true, wait: true);

                var history = await client.GetHistory(nameof(GetHistoryTest).ToLower(), 1);
                Assert.Single(history);

                var response = await client.UninstallRelease(nameof(GetHistoryTest).ToLower(), purge: false);
                Assert.Empty(response.Info);
                Assert.Null(response.Release);

                response = await client.UninstallRelease(nameof(GetHistoryTest).ToLower(), purge: true);
                Assert.Empty(response.Info);
                Assert.Null(response.Release);
            }
        }

        [Fact]
        public async Task GetReleaseContentTest()
        {
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(chartStream);

                var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());
                await Assert.ThrowsAsync<RpcException>(() => client.GetReleaseContent(nameof(GetReleaseContentTest).ToLower(), 0));

                await client.InstallReleaseAsync(chart.Serialize(), string.Empty, nameof(GetReleaseContentTest).ToLower(), true, wait: true);
                var response = await client.GetReleaseContent(nameof(GetReleaseContentTest).ToLower(), 0);
                Assert.NotNull(response);

                await client.UninstallRelease(nameof(GetReleaseContentTest).ToLower(), purge: true);
            }
        }

        [Fact]
        public async Task GetReleaseStatusTest()
        {
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(chartStream);

                var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());
                await Assert.ThrowsAsync<RpcException>(() => client.GetReleaseStatus(nameof(GetReleaseStatusTest).ToLower(), 0));

                await client.InstallReleaseAsync(chart.Serialize(), string.Empty, nameof(GetReleaseStatusTest).ToLower(), true, wait: true);
                var response = await client.GetReleaseStatus(nameof(GetReleaseStatusTest).ToLower(), 0);
                Assert.NotNull(response);

                await client.UninstallRelease(nameof(GetReleaseStatusTest).ToLower(), purge: true);
            }
        }

        [Fact]
        public async Task GetVersionStreamTest()
        {
            var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());
            var version = await client.GetVersionAsync();
            Assert.NotNull(version);
        }

        [Fact]
        public async Task InstallReleaseTest()
        {
            using (Stream chartStream = File.OpenRead("charts/hello-world-0.1.0.tgz"))
            {
                var chart = ChartPackage.Open(chartStream);
                var client = new TillerClient(() => TestConfiguration.GetStream().GetAwaiter().GetResult());

                var result = await client.InstallReleaseAsync(chart.Serialize(), string.Empty, nameof(InstallReleaseTest).ToLower(), true).ConfigureAwait(false);
            }
        }
    }
}
