namespace S22.Xmpp.Core.Sasl
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    internal class SaslException : Exception
    {
        public SaslException()
        {
        }

        public SaslException(string message) : base(message)
        {
        }

        protected SaslException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public SaslException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}

