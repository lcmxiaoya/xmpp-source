namespace S22.Xmpp.Extensions.Socks5
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    internal class Socks5Exception : Exception
    {
        public Socks5Exception()
        {
        }

        public Socks5Exception(string message) : base(message)
        {
        }

        protected Socks5Exception(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public Socks5Exception(string message, Exception inner) : base(message, inner)
        {
        }
    }
}

