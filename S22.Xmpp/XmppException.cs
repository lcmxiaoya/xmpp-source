namespace S22.Xmpp
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class XmppException : Exception
    {
        public XmppException()
        {
        }

        public XmppException(string message) : base(message)
        {
        }

        protected XmppException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public XmppException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}

