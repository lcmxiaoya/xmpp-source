namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(S22.Xmpp.Jid jid, S22.Xmpp.Im.Message message)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            message.ThrowIfNull<S22.Xmpp.Im.Message>("message");
            this.Jid = jid;
            this.Message = message;
        }

        public S22.Xmpp.Jid Jid { get; private set; }

        public S22.Xmpp.Im.Message Message { get; private set; }
    }
}

