using Google.Protobuf;
using Grpc.Core;
using Http2;
using Http2.Hpack;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Helm.Helm
{
    public class StreamCallInvoker : CallInvoker
    {
        private readonly Stream stream;
        private readonly CodedInputStream input;
        private readonly CodedOutputStream output;

        public StreamCallInvoker(Stream stream)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.input = new CodedInputStream(this.stream);
            this.output = new CodedOutputStream(this.stream);
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
            throw new NotImplementedException();
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

            var streams = this.stream.CreateStreams();

            Connection http2Connection = new Connection(
                config: config,
                inputStream: streams.ReadableStream,
                outputStream: streams.WriteableStream);

            HeaderField[] headers = new HeaderField[]
            {
                new HeaderField { Name = ":method", Value = "POST" },
                new HeaderField { Name = ":scheme", Value = "http" },
                new HeaderField { Name = ":path", Value = "/hapi.services.tiller.ReleaseService/GetVersion" },
                new HeaderField { Name = ":authority", Value = "pubsub.googleapis.com" },
                new HeaderField { Name = "grpc-timeout", Value = "1S" },
                new HeaderField {Name = "content-type", Value = "application/grpc+proto" }
            };

            var stream = http2Connection.CreateStreamAsync(
                headers, endOfStream: false).GetAwaiter().GetResult();

            var requestMessage = request as IMessage;
            byte[] buffer = new byte[4];

            stream.WriteAsync(new ArraySegment<byte>(buffer, 0, 1), endOfStream: false);
            int size = requestMessage.CalculateSize();
            buffer = BitConverter.GetBytes(size);
            stream.WriteAsync(new ArraySegment<byte>(buffer, 0, 4), endOfStream: size == 0);

            if (size > 0)
            {
                buffer = requestMessage.ToByteArray();
                stream.WriteAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), endOfStream: true);
            }

            // Wait for response headers
            var reponseHeaders = stream.ReadHeadersAsync().GetAwaiter().GetResult();

            // Read response data
            var response = Activator.CreateInstance<TResponse>();
            using (MemoryStream ms = new MemoryStream())
            {
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

                responseMessage.MergeFrom(ms);
            }

            return new AsyncUnaryCall<TResponse>(Task.FromResult(response), Task.FromResult((Metadata)null), () => Status.DefaultSuccess, null, null);
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string host, CallOptions options, TRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
