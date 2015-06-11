namespace MyChat.Client.Core.Exceptions
{
	using System;
	using System.Runtime.Serialization;

	class ServerUntrustedException: InvalidOperationException
    {
        public ServerUntrustedException() : base()
        {
        }

        public ServerUntrustedException(string message) : base(message)
        {
        }

        public ServerUntrustedException(string message, Exception inner) : base(message, inner)
        {
        }

        // This constructor is needed for serialization.
        protected ServerUntrustedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
