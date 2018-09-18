namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Drawing;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    public class AvatarChangedEventArgs : EventArgs
    {
        public AvatarChangedEventArgs(S22.Xmpp.Jid jid, string hash = null, Image avatar = null)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
            this.Hash = hash;
            this.Avatar = avatar;
        }

        public Image Avatar { get; private set; }

        public bool Cleared
        {
            get
            {
                return (this.Avatar == null);
            }
        }

        public string Hash { get; private set; }

        public S22.Xmpp.Jid Jid { get; private set; }
    }
}

