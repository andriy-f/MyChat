namespace MyChat.Client.Core.Exceptions
{
	using System;
	using System.Runtime.Serialization;

	public class ServerUntrustedException: InvalidOperationException
    {
        public ServerUntrustedException()
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
