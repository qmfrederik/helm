# .NET Client for Helm
[![Build status](https://ci.appveyor.com/api/projects/status/vsb2f1g32cj25h51/branch/master?svg=true)](https://ci.appveyor.com/project/qmfrederik/helm/branch/master) [![Build Status](https://travis-ci.org/qmfrederik/helm.svg?branch=master)](https://travis-ci.org/qmfrederik/helm)

[Helm](https://helm.sh/) is a package manager for Kubernetes. It allows you to install and use software built for Kubernetes.

This repository contains a C# client for working with Helm.

## Getting started
Before you start, add the [Helm NuGet package](https://www.nuget.org/packages/Helm/) to your project and make sure you've initialized
Helm inside your Kubernetes cluster.

Helm uses a Tiller pod deployed in your Kubernetes cluster to manage charts and deployments.

This means that you'll need to locate your Kubernetes cluster and the Tiller pod if you want to interact with it using C#.

Here's how you can do that:

```csharp
// Open the Kubernetes configuration and connect to the Kubernetes cluster.
// You may have a KUBECONFIG environment variable. If so, you're in luck - that's what we use in this sample app.
// You Kubernetes configuration can also be stored at ~/.kube/config.
var kubeConfig = KubernetesClientConfiguration.BuildConfigFromConfigFile(null, Environment.GetEnvironmentVariable("KUBECONFIG"));
var kubernetes = new Kubernetes(kubeConfig);

// Figure out where Tiller is located. This will return the IP address and port of a pod running Tiller.
// The code assumes you can connect to a pod using it's IP address. That usually means your code is either running inside a pod,
// or you've configured routing to your pod network.
// If that's not the case, you'll need to set up Kubernetes port forwarding.
TillerLocator locator = new TillerLocator(kubernetes);
var endPoint = locator.Locate();

// Connect to Tiller and get the version information
using(var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
{
    await socket.ConnectAsync(endPoint).ConfigureAwait(false);

    using (NetworkStream stream = new NetworkStream(socket))
    {
        TillerClient client = new TillerClient(stream);
        var version = await client.GetVersionAsync();
    }
}
```

## How it works
The Helm client consists of three blocks:
- A locator which helps you locate the Tiller pod in a Kubernetes cluster
- C# code for the Helm protobuf protocol. This protocol defines how messages which are sent to and received from Helm are serialized.
- A C# implementation of the gRPC protocol. This protocol allows to send and receive serialized messages over HTTP/2.