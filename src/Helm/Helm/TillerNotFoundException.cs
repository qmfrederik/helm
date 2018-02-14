using System;
using System.Runtime.Serialization;

namespace Helm.Helm
{
    [Serializable]
    public class TillerNotFoundException : Exception
    {
        public TillerNotFoundException()
            : this("Could not found the Tiller pod")
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
