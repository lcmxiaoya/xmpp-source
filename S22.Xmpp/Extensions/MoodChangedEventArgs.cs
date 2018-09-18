namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    public class MoodChangedEventArgs : EventArgs
    {
        public MoodChangedEventArgs(S22.Xmpp.Jid jid, S22.Xmpp.Extensions.Mood mood, string description = null)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
            this.Mood = mood;
            this.Description = description;
        }

        public string Description { get; private set; }

        public S22.Xmpp.Jid Jid { get; private set; }

        public S22.Xmpp.Extensions.Mood Mood { get; private set; }
    }
}

