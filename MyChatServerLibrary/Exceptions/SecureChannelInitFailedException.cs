namespace Andriy.MyChat.Server.Exceptions
{
    using System;

    using System.Runtime.Serialization;

    public class SecureChannelInitFailedException : InvalidOperationException
    {
        public SecureChannelInitFailedException() : base()
        {
        }

        public SecureChannelInitFailedException(string message) : base(message)
        {
        }

        public SecureChannelInitFailedException(string message, Exception inner) : base(message, inner)
        {
        }

        // This constructor is needed for serialization.
        protected SecureChannelInitFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
