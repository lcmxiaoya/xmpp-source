namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml;

    internal class XmlDictionary : IDictionary<string, string>, ICollection<KeyValuePair<string, string>>, IEnumerable<KeyValuePair<string, string>>, IEnumerable
    {
        private XmlElement element;
        private string key;
        private string tag;

        public XmlDictionary(XmlElement element, string tag, string key)
        {
            element.ThrowIfNull<XmlElement>("element");
            tag.ThrowIfNull<string>("tag");
            key.ThrowIfNull<string>("key");
            this.element = element;
            this.tag = tag;
            this.key = key;
        }

        public void Add(KeyValuePair<string, string> item)
        {
            this.Add(item.Key, item.Value);
        }

        public void Add(string key, string value)
        {
            key.ThrowIfNull<string>("key");
            if (this.ContainsKey(key))
            {
                throw new ArgumentException("An element with the same key already exists in the dictionary.");
            }
            if (this.element.IsReadOnly)
            {
                throw new NotSupportedException("The dictionary is read-only.");
            }
            this.element.Child(Xml.Element(this.tag, null).Attr(this.key, key).Text(value));
        }

        public void Clear()
        {
            if (this.element.IsReadOnly)
            {
                throw new NotSupportedException("The dictionary is read-only.");
            }
            ISet<XmlElement> set = new HashSet<XmlElement>();
            foreach (XmlElement element in this.element.GetElementsByTagName(this.tag))
            {
                if (!string.IsNullOrEmpty(element.GetAttribute(this.key)))
                {
                    set.Add(element);
                }
            }
            foreach (XmlElement element in set)
            {
                this.element.RemoveChild(element);
            }
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            XmlElement element = this.GetElement(item.Key);
            return ((element != null) && (element.InnerText == item.Value));
        }

        public bool ContainsKey(string key)
        {
            key.ThrowIfNull<string>("key");
            return (this.GetElement(key) != null);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            array.ThrowIfNull<KeyValuePair<string, string>[]>("array");
            if (arrayIndex < 0)
            {
                throw new IndexOutOfRangeException("arrayIndex");
            }
            int num = array.Length - arrayIndex;
            if (this.Count > num)
            {
                throw new ArgumentException();
            }
            foreach (KeyValuePair<string, string> pair in this)
            {
                array[arrayIndex++] = new KeyValuePair<string, string>(pair.Key, pair.Value);
            }
        }

        private XmlElement GetElement(string key)
        {
            key.ThrowIfNull<string>("key");
            foreach (XmlElement element in this.element.GetElementsByTagName(this.tag))
            {
                if (element.GetAttribute(this.key) == key)
                {
                    return element;
                }
            }
            return null;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (string str in this.Keys)
            {
                dictionary.Add(str, this[str]);
            }
            return dictionary.GetEnumerator();
        }

        public bool Remove(string key)
        {
            key.ThrowIfNull<string>("key");
            XmlElement oldChild = this.GetElement(key);
            if (oldChild != null)
            {
                this.element.RemoveChild(oldChild);
            }
            return (oldChild != null);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            XmlElement oldChild = this.GetElement(item.Key);
            if ((oldChild != null) && (oldChild.InnerText == item.Value))
            {
                this.element.RemoveChild(oldChild);
                return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool TryGetValue(string key, out string value)
        {
            key.ThrowIfNull<string>("key");
            XmlElement element = this.GetElement(key);
            value = (element != null) ? element.InnerText : null;
            return (element != null);
        }

        public int Count
        {
            get
            {
                return this.Keys.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.element.IsReadOnly;
            }
        }

        public string this[string key]
        {
            get
            {
                key.ThrowIfNull<string>("key");
                foreach (XmlElement element in this.element.GetElementsByTagName(this.tag))
                {
                    if (element.GetAttribute(this.key) == key)
                    {
                        return element.InnerText;
                    }
                }
                return null;
            }
            set
            {
                key.ThrowIfNull<string>("key");
                if (this.element.IsReadOnly)
                {
                    throw new NotSupportedException("The dictionary is read-only.");
                }
                XmlElement element = this.GetElement(key);
                if (element != null)
                {
                    element.InnerText = value;
                }
                else
                {
                    this.element.Child(Xml.Element(this.tag, null).Attr(this.key, key).Text(value));
                }
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                ISet<string> set = new HashSet<string>();
                foreach (XmlElement element in this.element.GetElementsByTagName(this.tag))
                {
                    string attribute = element.GetAttribute(this.key);
                    if (!string.IsNullOrEmpty(attribute))
                    {
                        set.Add(attribute);
                    }
                }
                return set;
            }
        }

        public ICollection<string> Values
        {
            get
            {
                ISet<string> set = new HashSet<string>();
                foreach (XmlElement element in this.element.GetElementsByTagName(this.tag))
                {
                    if (!string.IsNullOrEmpty(element.GetAttribute(this.key)))
                    {
                        set.Add(element.InnerText);
                    }
                }
                return set;
            }
        }
    }
}

