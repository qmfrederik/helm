using Google.Protobuf.Collections;
using Grpc.Core;
using Hapi.Chart;
using Hapi.Release;
using Hapi.Services.Tiller;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Hapi.Services.Tiller.ReleaseService;

namespace Helm.Helm
{
    public class TillerClient
    {
        private const string Version = "2.7.2";
        private readonly ReleaseServiceClient client;

        public TillerClient(string target = "127.0.0.1:44134")
        {
            this.client = new ReleaseServiceClient(new Channel(target, ChannelCredentials.Insecure));
        }

        public TillerClient(Channel channel)
        {
            this.client = new ReleaseServiceClient(channel);
        }

        public TillerClient(CallInvoker callInvoker)
        {
            this.client = new ReleaseServiceClient(callInvoker);
        }

        public TillerClient(Func<Stream> stream)
        {
            StreamCallInvoker invoker = new StreamCallInvoker(stream);
            this.client = new ReleaseServiceClient(invoker);
        }

        public async Task<List<Release>> GetHistory(string name, int max, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            GetHistoryRequest request = new GetHistoryRequest()
            {
                Max = max,
                Name = name
            };

            var response = await this.client.GetHistoryAsync(request, this.GetDefaultHeaders(), cancellationToken: cancellationToken);

            return response.Releases.ToList();
        }

        public async Task<Release> GetReleaseContent(string name, int version, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            GetReleaseContentRequest request = new GetReleaseContentRequest()
            {
                Name = name,
                Version = version
            };

            var response = await this.client.GetReleaseContentAsync(request, this.GetDefaultHeaders(), cancellationToken: cancellationToken);

            return response.Release;
        }

        public async Task<GetReleaseStatusResponse> GetReleaseStatus(string name, int version, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            GetReleaseStatusRequest request = new GetReleaseStatusRequest()
            {
                Name = name,
                Version = version
            };

            var response = await this.client.GetReleaseStatusAsync(request, this.GetDefaultHeaders(), cancellationToken: cancellationToken);

            return response;
        }

        public async Task<Hapi.Version.Version> GetVersion(CancellationToken cancellationToken = default(CancellationToken))
        {
            var version = await this.client.GetVersionAsync(new GetVersionRequest(), this.GetDefaultHeaders(), cancellationToken: cancellationToken);
            return version.Version;
        }

        public async Task<Hapi.Release.Release> InstallRelease(Chart chart, string values, string name, bool reuseName, string @namespace = "default", bool wait = false, CancellationToken cancellationToken = default(CancellationToken))
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
                Chart = chart,
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

        public async Task<List<Release>> ListReleases(
            string filter = "",
            int limit = 256,
            string @namespace = "",
            string offset = "",
            ListSort.Types.SortBy sortBy = ListSort.Types.SortBy.Name,
            ListSort.Types.SortOrder sortOrder = ListSort.Types.SortOrder.Asc,
            IEnumerable<Hapi.Release.Status.Types.Code> statusCodes = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ListReleasesRequest request = new ListReleasesRequest()
            {
                Filter = filter,
                Limit = limit,
                Namespace = @namespace,
                Offset = offset,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            if (statusCodes != null)
            {
                request.StatusCodes.AddRange(statusCodes);
            }

            var response = this.client.ListReleases(request, this.GetDefaultHeaders(), cancellationToken: cancellationToken);

            List<Release> releases = new List<Release>();

            while (!cancellationToken.IsCancellationRequested && await response.ResponseStream.MoveNext(cancellationToken))
            {
                releases.AddRange(response.ResponseStream.Current.Releases);
            }

            return releases;
        }

        public async Task<Release> RollbackRelease(string name, int version, bool dryRun = false, bool force = false, bool recreate = false, bool wait = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            RollbackReleaseRequest request = new RollbackReleaseRequest()
            {
                DryRun = dryRun,
                Force = force,
                Name = name,
                Recreate = recreate,
                Version = version,
                Wait = wait
            };

            var response = await this.client.RollbackReleaseAsync(request, this.GetDefaultHeaders(), cancellationToken: cancellationToken);

            return response.Release;
        }

        // TODO : Run release test
        public async Task<UninstallReleaseResponse> UninstallRelease(string name, bool purge = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            UninstallReleaseRequest request = new UninstallReleaseRequest()
            {
                Name = name,
                Purge = purge,
                DisableHooks = true
            };

            var response = await this.client.UninstallReleaseAsync(request, this.GetDefaultHeaders(), cancellationToken: cancellationToken);

            return response;
        }

        public async Task<Release> UpdateRelease(Chart chart, string values, string name, bool dryRun = false, bool force = false, bool recreate = false, bool resetValues = false, bool reuseValues = false, bool wait = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (chart == null)
            {
                throw new ArgumentNullException(nameof(chart));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            UpdateReleaseRequest request = new UpdateReleaseRequest()
            {
                Chart = chart,
                DryRun = dryRun,
                Force = force,
                Name = name,
                Recreate = recreate,
                ResetValues = resetValues,
                ReuseValues = reuseValues,
                Values = new Config()
                {
                    Raw = values
                },
                Wait = wait
            };

            var response = await this.client.UpdateReleaseAsync(request, this.GetDefaultHeaders(), cancellationToken: cancellationToken);

            return response.Release;
        }

        private Grpc.Core.Metadata GetDefaultHeaders()
        {
            var metadata = new Grpc.Core.Metadata();
            metadata.Add("x-helm-api-client", Version);
            return metadata;
        }
    }
}
