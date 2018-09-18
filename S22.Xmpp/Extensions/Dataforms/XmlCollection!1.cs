namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;

    internal class XmlCollection<T> : ICollection<T>, IEnumerable<T>, IEnumerable where T: class
    {
        private Func<XmlElement, T> conversion;
        private XmlElement element;
        private string tag;

        public XmlCollection(XmlElement element, string tag, Func<XmlElement, T> conversion)
        {
            element.ThrowIfNull<XmlElement>("element");
            tag.ThrowIfNull<string>("tag");
            conversion.ThrowIfNull<Func<XmlElement, T>>("conversion");
            this.element = element;
            this.tag = tag;
            this.conversion = conversion;
            try
            {
                this.GetItems();
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The specified element contained invalid data.", exception);
            }
        }

        public void Add(T item)
        {
            item.ThrowIfNull<T>("item");
            XmlElement child = Xml.Element(this.tag, null);
            try
            {
                child.InnerXml = item.ToString();
            }
            catch (XmlException exception)
            {
                throw new ArgumentException("The specified item could not be serialized into XML.", exception);
            }
            this.element.Child(child);
        }

        public void Clear()
        {
            foreach (XmlElement element in this.GetElements())
            {
                this.element.RemoveChild(element);
            }
        }

        public bool Contains(T item)
        {
            item.ThrowIfNull<T>("item");
            foreach (T local in this.GetItems())
            {
                if (item.Equals(local))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        private ICollection<XmlElement> GetElements()
        {
            ISet<XmlElement> set = new HashSet<XmlElement>();
            foreach (XmlElement element in this.element.GetElementsByTagName(this.tag))
            {
                if (element.ParentNode == this.element)
                {
                    set.Add(element);
                }
            }
            return set;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.GetItems().GetEnumerator();
        }

        private ICollection<T> GetItems()
        {
            ISet<T> set = new HashSet<T>();
            foreach (XmlElement element in this.element.GetElementsByTagName(this.tag))
            {
                if (element.ParentNode == this.element)
                {
                    try
                    {
                        set.Add(this.conversion(element));
                    }
                    catch (Exception exception)
                    {
                        throw new XmlException("Could not convert XML element into an instance of type " + typeof(T) + ".", exception);
                    }
                }
            }
            return set;
        }

        public bool Remove(T item)
        {
            item.ThrowIfNull<T>("item");
            XmlElement oldChild = null;
            foreach (XmlElement element2 in this.GetElements())
            {
                if (element2.InnerText == item.ToString())
                {
                    oldChild = element2;
                }
            }
            if (oldChild == null)
            {
                return false;
            }
            this.element.RemoveChild(oldChild);
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override string ToString()
        {
            return this.element.ToXmlString(false, false);
        }

        public int Count
        {
            get
            {
                return this.GetElements().Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.element.IsReadOnly;
            }
        }
    }
}

