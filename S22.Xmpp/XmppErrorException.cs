namespace S22.Xmpp
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;

    [Serializable]
    public class XmppErrorException : Exception
    {
        public XmppErrorException(XmppError error)
        {
            error.ThrowIfNull<XmppError>("error");
            this.Error = error;
        }

        public XmppErrorException(XmppError error, string message) : base(message)
        {
            error.ThrowIfNull<XmppError>("error");
            this.Error = error;
        }

        protected XmppErrorException(XmppError error, SerializationInfo info, StreamingContext context) : base(info, context)
        {
            error.ThrowIfNull<XmppError>("error");
            this.Error = error;
        }

        public XmppErrorException(XmppError error, string message, Exception inner) : base(message, inner)
        {
            error.ThrowIfNull<XmppError>("error");
            this.Error = error;
        }

        public XmppError Error { get; private set; }
    }
}

