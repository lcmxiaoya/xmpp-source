namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    public class UnsubscribedEventArgs : EventArgs
    {
        public UnsubscribedEventArgs(S22.Xmpp.Jid jid)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
        }

        public S22.Xmpp.Jid Jid { get; private set; }
    }
}

