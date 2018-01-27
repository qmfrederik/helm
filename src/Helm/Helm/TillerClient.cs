using Grpc.Core;
using Hapi.Services.Tiller;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Hapi.Services.Tiller.ReleaseService;

namespace Helm.Helm
{
    public class TillerClient
    {
        private readonly string target;

        public TillerClient(string target = "127.0.0.1:44134")
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public async Task<Hapi.Version.Version> GetVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var channel = new Channel(this.target, ChannelCredentials.Insecure);
            var releaseServiceClient = new ReleaseServiceClient(channel);
            var version = await releaseServiceClient.GetVersionAsync(new GetVersionRequest(), cancellationToken: cancellationToken);
            return version.Version;
        }
    }
}
