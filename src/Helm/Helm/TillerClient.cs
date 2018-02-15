using Grpc.Core;
using Hapi.Services.Tiller;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Hapi.Services.Tiller.ReleaseService;

namespace Helm.Helm
{
    public class TillerClient
    {
        private readonly ReleaseServiceClient client;
        private const string Version = "2.7.2";

        public TillerClient(string target = "127.0.0.1:44134")
        {
            this.client = new ReleaseServiceClient(new Channel(target, ChannelCredentials.Insecure));
        }

        public TillerClient(CallInvoker callInvoker)
        {
            this.client = new ReleaseServiceClient(callInvoker);
        }

        public TillerClient(Stream stream)
        {
            StreamCallInvoker invoker = new StreamCallInvoker(stream);
            this.client = new ReleaseServiceClient(invoker);
        }

        public async Task<Hapi.Version.Version> GetVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var version = await this.client.GetVersionAsync(new GetVersionRequest(), this.GetDefaultHeaders(), cancellationToken: cancellationToken);
            return version.Version;
        }

        public async Task<Hapi.Release.Release> InstallReleaseAsync(Charts.Chart chart, string values, string name, bool reuseName, string @namespace = "default", bool wait = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (chart == null)
            {
                throw new ArgumentNullException(nameof(chart));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(@namespace))
            {
                throw new ArgumentNullException(nameof(@namespace));
            }

            InstallReleaseRequest request = new InstallReleaseRequest()
            {
                Chart = chart.Serialize(),
                Name = name,
                Namespace = @namespace,
                ReuseName = reuseName,
                Values = new Hapi.Chart.Config()
                {
                    Raw = values
                },
                Wait = wait
            };

            var response = await this.client.InstallReleaseAsync(request, this.GetDefaultHeaders(), cancellationToken: cancellationToken);
            return response.Release;
        }

        private Metadata GetDefaultHeaders()
        {
            var metadata = new Metadata();
            metadata.Add("x-helm-api-client", Version);
            return metadata;
        }
    }
}
