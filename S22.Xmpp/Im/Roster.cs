namespace S22.Xmpp.Im
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public class Roster : IEnumerable<RosterItem>, IEnumerable
    {
        private ISet<RosterItem> items = new HashSet<RosterItem>();

        internal Roster(IEnumerable<RosterItem> items = null)
        {
            if (items != null)
            {
                foreach (RosterItem item in items)
                {
                    this.items.Add(item);
                }
            }
        }

        internal bool Add(RosterItem item)
        {
            return this.items.Add(item);
        }

        public IEnumerator<RosterItem> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        public int Count
        {
            get
            {
                return this.items.Count;
            }
        }
    }
}

