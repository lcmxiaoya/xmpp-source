namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    public class IqEventArgs : EventArgs
    {
        public IqEventArgs(Iq stanza)
        {
            stanza.ThrowIfNull<Iq>("stanza");
            this.Stanza = stanza;
        }

        public Iq Stanza { get; private set; }
    }
}

