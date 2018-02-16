using Helm.Helm;
using k8s;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Helm.IntegrationTests
{
    internal static class TestConfiguration
    {
        public static KubernetesClientConfiguration Configure()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBECONFIG")))
            {
                return KubernetesClientConfiguration.BuildConfigFromConfigFile(null, Environment.GetEnvironmentVariable("KUBECONFIG"));
            }
            if (File.Exists("minikube.config"))
            {
                // If you're using minikube, things can get akward if you import the root CA in the Trusted Root Certificate Authorities list
                // and re-create your cluster. Certificates issued will be rejected by Windows because the DN of the root CA doesn't change;
                // yet the certificate will have a different signature.
                return KubernetesClientConfiguration.BuildConfigFromConfigFile(null, "minikube.config");
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Task<IPEndPoint> GetEndPoint()
        {
            var configuration = Configure();
            var kubernetes = new Kubernetes(configuration);
            TillerLocator locator = new TillerLocator(kubernetes);
            return locator.Locate();
        }

        public static async Task<Socket> GetSocket()
        {
            var endPoint = await GetEndPoint().ConfigureAwait(false);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endPoint).ConfigureAwait(false);
            return socket;
        }

        public static async Task<Stream> GetStream()
        {
            var socket = await GetSocket().ConfigureAwait(false);
            var stream = new NetworkStream(socket, ownsSocket: true);
            return stream;
        }
    }
}
