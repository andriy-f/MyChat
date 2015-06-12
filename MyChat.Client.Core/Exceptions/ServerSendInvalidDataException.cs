namespace MyChat.Client.Core.Exceptions
{
	using System;
	using System.Runtime.Serialization;

	public class ServerSendInvalidDataException: InvalidOperationException
    {
        public ServerSendInvalidDataException()
        {
        }

        public ServerSendInvalidDataException(string message) : base(message)
        {
        }

        public ServerSendInvalidDataException(string message, Exception inner) : base(message, inner)
        {
        }

        // This constructor is needed for serialization.
        protected ServerSendInvalidDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
