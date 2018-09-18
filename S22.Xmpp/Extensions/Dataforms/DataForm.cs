namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Runtime.InteropServices;
    using System.Xml;

    public abstract class DataForm
    {
        protected XmlElement element;
        private FieldList fields;

        internal DataForm(XmlElement element, bool readOnly = false)
        {
            element.ThrowIfNull<XmlElement>("element");
            this.element = element;
            try
            {
                this.fields = new FieldList(element, readOnly);
                this.GetDataFormType();
            }
            catch (XmlException exception)
            {
                throw new ArgumentException("The element parameter is not a valid data-form.", exception);
            }
        }

        internal DataForm(string title = null, string instructions = null, bool readOnly = false, params DataField[] fields)
        {
            this.element = Xml.Element("x", "jabber:x:data");
            this.Title = title;
            this.Instructions = instructions;
            this.fields = new FieldList(this.element, readOnly);
            if (fields != null)
            {
                foreach (DataField field in fields)
                {
                    if (field != null)
                    {
                        this.fields.Add(field);
                    }
                }
            }
        }

        protected void AssertType(DataFormType expected)
        {
            if (this.Type != expected)
            {
                throw new ArgumentException("The specified XML element is not a data-form of type '" + expected.ToString() + "'.");
            }
        }

        private DataFormType GetDataFormType()
        {
            DataFormType type;
            try
            {
                type = Util.ParseEnum<DataFormType>(this.element.GetAttribute("type"), true);
            }
            catch (Exception exception)
            {
                throw new XmlException("The 'type' attribute of the underlying XML element is invalid.", exception);
            }
            return type;
        }

        public override string ToString()
        {
            return this.element.ToXmlString(false, false);
        }

        public XmlElement ToXmlElement()
        {
            return this.element;
        }

        public FieldList Fields
        {
            get
            {
                return this.fields;
            }
        }

        public string Instructions
        {
            get
            {
                if (this.element["instructions"] != null)
                {
                    return this.element["instructions"].InnerText;
                }
                return null;
            }
            set
            {
                XmlElement oldChild = this.element["instructions"];
                if (oldChild != null)
                {
                    if (value == null)
                    {
                        this.element.RemoveChild(oldChild);
                    }
                    else
                    {
                        oldChild.InnerText = value;
                    }
                }
                else if (value != null)
                {
                    this.element.Child(Xml.Element("instructions", null).Text(value));
                }
            }
        }

        public string Title
        {
            get
            {
                if (this.element["title"] != null)
                {
                    return this.element["title"].InnerText;
                }
                return null;
            }
            set
            {
                XmlElement oldChild = this.element["title"];
                if (oldChild != null)
                {
                    if (value == null)
                    {
                        this.element.RemoveChild(oldChild);
                    }
                    else
                    {
                        oldChild.InnerText = value;
                    }
                }
                else if (value != null)
                {
                    this.element.Child(Xml.Element("title", null).Text(value));
                }
            }
        }

        public DataFormType Type
        {
            get
            {
                return this.GetDataFormType();
            }
            protected set
            {
                this.element.SetAttribute("type", value.ToString().ToLower());
            }
        }
    }
}

