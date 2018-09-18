namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    [Serializable]
    public class ChatStateChangedEventArgs : EventArgs
    {
        public ChatStateChangedEventArgs(S22.Xmpp.Jid jid, S22.Xmpp.Extensions.ChatState state)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
            this.ChatState = state;
        }

        public S22.Xmpp.Extensions.ChatState ChatState { get; private set; }

        public S22.Xmpp.Jid Jid { get; private set; }
    }
}

