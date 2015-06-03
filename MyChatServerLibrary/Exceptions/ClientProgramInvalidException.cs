namespace Andriy.MyChat.Server.Exceptions
{
    using System;

    using System.Runtime.Serialization;

    public class ClientProgramInvalidException : InvalidOperationException
    {
        public ClientProgramInvalidException() : base()
        {
        }

        public ClientProgramInvalidException(string message) : base(message)
        {
        }

        public ClientProgramInvalidException(string message, Exception inner) : base(message, inner)
        {
        }

        // This constructor is needed for serialization.
        protected ClientProgramInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
