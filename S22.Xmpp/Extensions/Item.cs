namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    internal class Item
    {
        public Item(S22.Xmpp.Jid jid, string node = null, string name = null)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
            this.Node = node;
            this.Name = name;
        }

        public S22.Xmpp.Jid Jid { get; private set; }

        public string Name { get; private set; }

        public string Node { get; private set; }
    }
}

