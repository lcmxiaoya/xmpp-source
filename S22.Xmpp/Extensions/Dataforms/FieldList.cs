namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;

    public class FieldList : IEnumerable<DataField>, IEnumerable
    {
        private XmlElement element;
        private bool readOnly;

        public FieldList(XmlElement element, bool readOnly = false)
        {
            element.ThrowIfNull<XmlElement>("element");
            this.element = element;
            this.readOnly = readOnly;
            try
            {
                this.GetFields();
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The specified XML element is not a valid data-form.", exception);
            }
        }

        public void Add(DataField item)
        {
            item.ThrowIfNull<DataField>("item");
            if (this.IsReadOnly)
            {
                throw new NotSupportedException("The list is read-only.");
            }
            if ((item.Name != null) && this.Contains(item.Name))
            {
                throw new ArgumentException("A field with the same name already exists.");
            }
            this.element.Child(item.ToXmlElement());
        }

        private DataFieldType AttributeValueToType(string value)
        {
            value.ThrowIfNull<string>("value");
            StringBuilder builder = new StringBuilder();
            string str = value;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '-')
                {
                    builder.Append(char.ToUpper(str[++i]));
                }
                else
                {
                    builder.Append(str[i]);
                }
            }
            value = builder.ToString();
            return Util.ParseEnum<DataFieldType>(value, true);
        }

        public void Clear()
        {
            if (this.IsReadOnly)
            {
                throw new NotSupportedException("The list is read-only.");
            }
            ISet<XmlElement> set = new HashSet<XmlElement>();
            foreach (XmlElement element in this.element.GetElementsByTagName("field"))
            {
                if (element.ParentNode == this.element)
                {
                    set.Add(element);
                }
            }
            foreach (XmlElement element in set)
            {
                this.element.RemoveChild(element);
            }
        }

        public bool Contains(string name)
        {
            name.ThrowIfNull<string>("name");
            foreach (DataField field in this.GetFields())
            {
                if (field.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        private DataField FieldFromXml(XmlElement element)
        {
            DataField field;
            element.ThrowIfNull<XmlElement>("element");
            try
            {
                DataFieldType? dataFieldType = this.GetDataFieldType(element);
                if (!dataFieldType.HasValue)
                {
                    return new DataField(element);
                }
                switch (dataFieldType.Value)
                {
                    case DataFieldType.Boolean:
                        return new BooleanField(element);

                    case DataFieldType.Fixed:
                        return new FixedField(element);

                    case DataFieldType.Hidden:
                        return new HiddenField(element);

                    case DataFieldType.JidMulti:
                        return new JidMultiField(element);

                    case DataFieldType.JidSingle:
                        return new JidField(element);

                    case DataFieldType.ListMulti:
                        return new ListMultiField(element);

                    case DataFieldType.ListSingle:
                        return new ListField(element);

                    case DataFieldType.TextMulti:
                        return new TextMultiField(element);

                    case DataFieldType.TextSingle:
                        return new TextField(element);

                    case DataFieldType.TextPrivate:
                        return new PasswordField(element);
                }
                throw new XmlException("Invalid 'type' attribute: " + dataFieldType);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("Invalid 'field' XML element.", exception);
            }
            return field;
        }

        private DataFieldType? GetDataFieldType(XmlElement element)
        {
            DataFieldType? nullable;
            try
            {
                string attribute = element.GetAttribute("type");
                if (string.IsNullOrEmpty(attribute))
                {
                    return null;
                }
                nullable = new DataFieldType?(this.AttributeValueToType(attribute));
            }
            catch (Exception exception)
            {
                throw new XmlException("The 'type' attribute of the underlying XML element is invalid.", exception);
            }
            return nullable;
        }

        public IEnumerator<DataField> GetEnumerator()
        {
            return this.GetFields().GetEnumerator();
        }

        private XmlElement GetFieldElementByName(string name)
        {
            name.ThrowIfNull<string>("name");
            foreach (XmlElement element in this.GetFieldElements())
            {
                if (element.GetAttribute("var") == name)
                {
                    return element;
                }
            }
            return null;
        }

        private IList<XmlElement> GetFieldElements()
        {
            IList<XmlElement> list = new List<XmlElement>();
            foreach (XmlElement element in this.element.GetElementsByTagName("field"))
            {
                if (element.ParentNode == this.element)
                {
                    list.Add(element);
                }
            }
            return list;
        }

        private IList<DataField> GetFields()
        {
            IList<DataField> list = new List<DataField>();
            foreach (XmlElement element in this.GetFieldElements())
            {
                list.Add(this.FieldFromXml(element));
            }
            return list;
        }

        public void Remove(DataField item)
        {
            item.ThrowIfNull<DataField>("item");
            this.Remove(item.Name);
        }

        public void Remove(string name)
        {
            if (name != null)
            {
                XmlElement fieldElementByName = this.GetFieldElementByName(name);
                if (fieldElementByName != null)
                {
                    this.element.RemoveChild(fieldElementByName);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public int Count
        {
            get
            {
                return this.GetFieldElements().Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return this.readOnly;
            }
        }

        public DataField this[int index]
        {
            get
            {
                IList<DataField> fields = this.GetFields();
                index.ThrowIfOutOfRange(0, fields.Count - 1);
                return fields[index];
            }
        }

        public DataField this[string name]
        {
            get
            {
                foreach (DataField field in this.GetFields())
                {
                    if (field.Name == name)
                    {
                        return field;
                    }
                }
                return null;
            }
        }
    }
}

