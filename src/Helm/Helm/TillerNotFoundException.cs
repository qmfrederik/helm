using System;
using System.Runtime.Serialization;

namespace Helm.Helm
{
    [Serializable]
    public class TillerNotFoundException : Exception
    {
        public TillerNotFoundException()
            : this("Could not find the Tiller pod in the Kubernetes cluster. Make sure Helm has been installed correctly.")
        {
        }

        public TillerNotFoundException(string message)
            : base(message)
        {
        }

        public TillerNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected TillerNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
