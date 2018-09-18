namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    public class TuneEventArgs : EventArgs
    {
        public TuneEventArgs(S22.Xmpp.Jid jid, TuneInformation information = null)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
            this.Information = information;
        }

        public TuneInformation Information { get; private set; }

        public S22.Xmpp.Jid Jid { get; private set; }

        public bool Stop
        {
            get
            {
                return (this.Information == null);
            }
        }
    }
}

