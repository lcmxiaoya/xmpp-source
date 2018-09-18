namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    public class RosterUpdatedEventArgs : EventArgs
    {
        public RosterUpdatedEventArgs(RosterItem item, bool removed)
        {
            item.ThrowIfNull<RosterItem>("item");
            this.Item = item;
            this.Removed = removed;
        }

        public RosterItem Item { get; private set; }

        public bool Removed { get; private set; }
    }
}

