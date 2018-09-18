namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(Message stanza)
        {
            stanza.ThrowIfNull<Message>("stanza");
            this.Stanza = stanza;
        }

        public Message Stanza { get; private set; }
    }
}

