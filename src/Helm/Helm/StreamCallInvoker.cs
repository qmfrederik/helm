using Google.Protobuf;
using Grpc.Core;
using Http2;
using Http2.Hpack;
using Http2.Internal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Helm.Helm
{
    public class StreamCallInvoker : CallInvoker
    {
        private readonly Func<Stream> stream;
        private readonly ILogger logger;

        public StreamCallInvoker(Func<Stream> stream, ILogger<StreamCallInvoker> logger = null)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.logger = logger;
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            throw new NotImplementedException();
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options)
        {
            throw new NotImplementedException();
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            var unaryCall = this.AsyncUnaryCall(method, host, options, request);

            return new AsyncServerStreamingCall<TResponse>(
                responseStream: new AsyncStreamReader<TResponse>(unaryCall),
                responseHeadersAsync: unaryCall.ResponseHeadersAsync,
                getStatusFunc: unaryCall.GetStatus,
                getTrailersFunc: unaryCall.GetTrailers,
                disposeAction: unaryCall.Dispose);
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            // The HTTP/2 gRPC protocol is defined here
            // https://github.com/grpc/grpc/blob/master/doc/PROTOCOL-HTTP2.md
            // When debugging, be aware of the timeouts!
            // You can get some insights about what's going on by running kubectl logs <tiller> -n kube-system -f
            ConnectionConfiguration config =
               new ConnectionConfigurationBuilder(isServer: false)
               .Build();

            using (Stream networkStream = this.stream())
            {
                var streams = networkStream.CreateStreams();

                Connection http2Connection = new Connection(
                    config: config,
                    inputStream: streams.ReadableStream,
                    outputStream: streams.WriteableStream);

                var headers = new Collection<HeaderField>
                {
                    new HeaderField { Name = ":method", Value = "POST" },
                    new HeaderField { Name = ":scheme", Value = "http" },
                    new HeaderField { Name = ":path", Value = method.FullName },
                    new HeaderField { Name = ":authority", Value = "pubsub.googleapis.com" },
                    new HeaderField { Name = "grpc-timeout", Value = "60S" },
                    new HeaderField { Name = "content-type", Value = "application/grpc+proto" }
                };

                if (options.Headers != null)
                {
                    foreach (var header in options.Headers)
                    {
                        headers.Add(new HeaderField()
                        {
                            Name = header.Key,
                            Value = header.Value
                        });
                    }
                }

                var stream = http2Connection.CreateStreamAsync(
                    headers, endOfStream: false).GetAwaiter().GetResult();

                var requestMessage = request as IMessage;
                byte[] buffer = new byte[4];

                stream.WriteAsync(new ArraySegment<byte>(buffer, 0, 1), endOfStream: false);
                int size = requestMessage.CalculateSize();
                buffer = BitConverter.GetBytes(size);
                Array.Reverse(buffer);
                stream.WriteAsync(new ArraySegment<byte>(buffer, 0, 4), endOfStream: size == 0);

                if (size > 0)
                {
                    buffer = requestMessage.ToByteArray();
                    stream.WriteAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), endOfStream: true);
                }

                // Wait for response headers
                var responseHeaders = stream.ReadHeadersAsync().GetAwaiter().GetResult();

                if (this.logger != null)
                {
                    foreach (var header in responseHeaders)
                    {
                        this.logger.LogTrace("{header.Name} = {header.Value}");
                    }
                }

                var statusCode = responseHeaders.SingleOrDefault(h => h.Name == ":status").Value;
                var contentType = responseHeaders.SingleOrDefault(h => h.Name == "content-type").Value;
                var grpcStatusCodeString = responseHeaders.SingleOrDefault(h => h.Name == "grpc-status").Value;
                var grpcMessage = responseHeaders.SingleOrDefault(h => h.Name == "grpc-message").Value;

                var grpcStatusCode = grpcStatusCodeString == null ? StatusCode.OK : (StatusCode)Enum.Parse(typeof(StatusCode), grpcStatusCodeString);

                // Read response data
                var response = Activator.CreateInstance<TResponse>();

                using (MemoryStream ms = new MemoryStream())
                {
                    // See https://github.com/Matthias247/http2dotnet/issues/1
                    var streamImplType = typeof(IStream).Assembly.GetType("Http2.StreamImpl");
                    var readDataPossibleField = streamImplType.GetField("readDataPossible", BindingFlags.NonPublic | BindingFlags.Instance);
                    var readDataPossible = (AsyncManualResetEvent)readDataPossibleField.GetValue(stream);
                    readDataPossible.Set();

                    buffer = new byte[1024];
                    while (true)
                    {
                        var readDataResult = stream.ReadAsync(new ArraySegment<byte>(buffer)).GetAwaiter().GetResult();
                        ms.Write(buffer, 0, readDataResult.BytesRead);

                        if (readDataResult.EndOfStream)
                        {
                            break;
                        }
                    }

                    var responseMessage = response as IMessage;

                    ms.Position = 0;
                    bool isCompressed = ms.ReadByte() != 0;
                    byte[] lengthBuffer = new byte[4];
                    ms.Read(lengthBuffer, 0, 4);
                    Array.Reverse(lengthBuffer);
                    var length = BitConverter.ToUInt32(lengthBuffer, 0);
                    this.logger?.LogTrace("Read {length} bytes of data");

                    responseMessage.MergeFrom(ms);
                }

                var responseTrailers = stream.ReadTrailersAsync().GetAwaiter().GetResult();

                if (grpcStatusCode != StatusCode.OK)
                {
                    var status = new Status(grpcStatusCode, grpcMessage);
                    var metadata = new Metadata();

                    foreach (var trailer in responseTrailers)
                    {
                        metadata.Add(new Metadata.Entry(trailer.Name, trailer.Value));
                    }

                    throw new Grpc.Core.RpcException(status, metadata);
                }

                return new AsyncUnaryCall<TResponse>(Task.FromResult(response), Task.FromResult((Metadata)null), () => Status.DefaultSuccess, null, null);
            }
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
