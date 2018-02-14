using k8s;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Helm.Helm
{
    public class TillerLocator
    {
        private readonly IKubernetes kubernetes;

        public TillerLocator(IKubernetes kubernetes)
        {
            this.kubernetes = kubernetes ?? throw new ArgumentNullException(nameof(kubernetes));
        }

        public async Task<IPEndPoint> Locate(string @namespace = "kube-system")
        {
            var tillerPods = await this.kubernetes.ListNamespacedPodAsync(@namespace, labelSelector: "app=helm,name=tiller").ConfigureAwait(false);

            if (tillerPods.Items.Count == 0)
            {
                throw new TillerNotFoundException();
            }

            var tillerPod = tillerPods.Items[0];
            return new IPEndPoint(IPAddress.Parse(tillerPod.Status.PodIP), 44134);
        }
    }
}
