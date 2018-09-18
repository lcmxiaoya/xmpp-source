namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    public class SubscriptionRequestEventArgs : EventArgs
    {
        public SubscriptionRequestEventArgs(S22.Xmpp.Jid jid)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
        }

        public S22.Xmpp.Jid Jid { get; private set; }
    }
}

