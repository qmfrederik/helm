using k8s;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Helm.Helm
{
    public class TillerLocator
    {
        private readonly IKubernetes kubernetes;

        public TillerLocator(KubernetesClientConfiguration configuration)
            : this(new Kubernetes(configuration))
        {
        }

        public TillerLocator(IKubernetes kubernetes)
        {
            this.kubernetes = kubernetes ?? throw new ArgumentNullException(nameof(kubernetes));
        }

        public static Task<Stream> Connect(KubernetesClientConfiguration configuration, string @namespace = "kube-system", CancellationToken cancellationToken = default(CancellationToken))
        {
            TillerLocator locator = new TillerLocator(configuration);
            return locator.Connect(@namespace, cancellationToken);
        }

        public async Task<IPEndPoint> Locate(string @namespace = "kube-system", CancellationToken cancellationToken = default(CancellationToken))
        {
            var tillerPods = await this.kubernetes.ListNamespacedPodAsync(@namespace, labelSelector: "app=helm,name=tiller", cancellationToken: cancellationToken).ConfigureAwait(false);

            if (tillerPods.Items.Count == 0)
            {
                throw new TillerNotFoundException();
            }

            var tillerPod = tillerPods.Items[0];
            return new IPEndPoint(IPAddress.Parse(tillerPod.Status.PodIP), 44134);
        }

        public async Task<Stream> Connect(string @namespace = "kube-system", CancellationToken cancellationToken = default(CancellationToken))
        {
            var endPoint = await this.Locate(@namespace, cancellationToken).ConfigureAwait(false);

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endPoint).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
    }
}
