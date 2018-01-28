using Grpc.Core;
using Hapi.Services.Tiller;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Hapi.Services.Tiller.ReleaseService;

namespace Helm.Helm
{
    public class TillerClient
    {
        private readonly ReleaseServiceClient client;

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
            var version = await this.client.GetVersionAsync(new GetVersionRequest(), cancellationToken: cancellationToken);
            return version.Version;
        }
    }
}
