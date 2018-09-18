namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class PrivacyList : ICollection<PrivacyRule>, IEnumerable<PrivacyRule>, IEnumerable
    {
        private ISet<PrivacyRule> rules = new HashSet<PrivacyRule>();

        public PrivacyList(string name)
        {
            name.ThrowIfNull<string>("name");
            this.Name = name;
        }

        public void Add(PrivacyRule item)
        {
            item.ThrowIfNull<PrivacyRule>("item");
            foreach (PrivacyRule rule in this.rules)
            {
                if (rule.Order == item.Order)
                {
                    throw new ArgumentException("A rule with an order value of " + rule.Order + " already exists.");
                }
            }
            this.rules.Add(item);
        }

        public uint Add(PrivacyRule item, bool overWriteOrder)
        {
            item.ThrowIfNull<PrivacyRule>("item");
            uint? nullable = null;
            foreach (PrivacyRule rule in this.rules)
            {
                if (!(overWriteOrder || (rule.Order != item.Order)))
                {
                    throw new ArgumentException("A rule with an order value of " + rule.Order + " already exists.");
                }
                if (!nullable.HasValue)
                {
                    nullable = new uint?(rule.Order);
                }
                if (rule.Order > nullable)
                {
                    nullable = new uint?(rule.Order);
                }
            }
            if (nullable.HasValue)
            {
                item.Order = nullable.Value + 1;
            }
            this.rules.Add(item);
            return item.Order;
        }

        public void Clear()
        {
            this.rules.Clear();
        }

        public bool Contains(PrivacyRule item)
        {
            item.ThrowIfNull<PrivacyRule>("item");
            return this.rules.Contains(item);
        }

        public void CopyTo(PrivacyRule[] array, int arrayIndex)
        {
            array.ThrowIfNull<PrivacyRule[]>("array");
            this.rules.CopyTo(array, arrayIndex);
        }

        public IEnumerator<PrivacyRule> GetEnumerator()
        {
            return this.rules.GetEnumerator();
        }

        public bool Remove(PrivacyRule item)
        {
            item.ThrowIfNull<PrivacyRule>("item");
            return this.rules.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.rules.GetEnumerator();
        }

        public int Count
        {
            get
            {
                return this.rules.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.rules.IsReadOnly;
            }
        }

        public string Name { get; private set; }
    }
}

