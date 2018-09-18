namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using System;
    using System.Runtime.CompilerServices;

    public class IqEventArgs : EventArgs
    {
        public IqEventArgs(S22.Xmpp.Jid from, Iq iqInfo)
        {
            this.Jid = from;
            this.IqInfo = iqInfo;
        }

        public Iq IqInfo { get; private set; }

        public S22.Xmpp.Jid Jid { get; private set; }
    }
}

