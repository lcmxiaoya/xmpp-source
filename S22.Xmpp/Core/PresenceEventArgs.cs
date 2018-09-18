namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    public class PresenceEventArgs : EventArgs
    {
        public PresenceEventArgs(Presence stanza)
        {
            stanza.ThrowIfNull<Presence>("stanza");
            this.Stanza = stanza;
        }

        public Presence Stanza { get; private set; }
    }
}

