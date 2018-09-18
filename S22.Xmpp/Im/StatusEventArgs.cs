namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    public class StatusEventArgs : EventArgs
    {
        public StatusEventArgs(S22.Xmpp.Jid jid, S22.Xmpp.Im.Status status)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            status.ThrowIfNull<S22.Xmpp.Im.Status>("status");
            this.Jid = jid;
            this.Status = status;
        }

        public S22.Xmpp.Jid Jid { get; private set; }

        public S22.Xmpp.Im.Status Status { get; private set; }
    }
}

