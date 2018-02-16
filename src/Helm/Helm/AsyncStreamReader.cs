using Grpc.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Helm.Helm
{
    internal class AsyncStreamReader<T> : IAsyncStreamReader<T>
    {
        private readonly AsyncUnaryCall<T> unaryCall;
        private bool read = false;

        public AsyncStreamReader(AsyncUnaryCall<T> unaryCall)
        {
            this.unaryCall = unaryCall ?? throw new ArgumentNullException(nameof(unaryCall));
        }

        public T Current
        {
            get;
            set;
        }

        public void Dispose()
        {
        }

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (!this.read)
            {
                this.Current = await this.unaryCall.ResponseAsync.ConfigureAwait(false);
                this.read = true;
                return true;
            }

            return false;
        }
    }
}
