namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public class RosterItem
    {
        private ISet<string> groups;

        public RosterItem(S22.Xmpp.Jid jid, string name = null, params string[] groups) : this(jid, name, S22.Xmpp.Im.SubscriptionState.None, false, groups)
        {
        }

        internal RosterItem(S22.Xmpp.Jid jid, string name, S22.Xmpp.Im.SubscriptionState state, bool pending, IEnumerable<string> groups)
        {
            this.groups = new HashSet<string>();
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
            this.Name = name;
            if (groups != null)
            {
                foreach (string str in groups)
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        this.groups.Add(str);
                    }
                }
            }
            this.SubscriptionState = state;
            this.Pending = pending;
        }

        public IEnumerable<string> Groups
        {
            get
            {
                return this.groups;
            }
        }

        public S22.Xmpp.Jid Jid { get; private set; }

        public string Name { get; private set; }

        public bool Pending { get; private set; }

        public S22.Xmpp.Im.SubscriptionState SubscriptionState { get; private set; }
    }
}

