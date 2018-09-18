namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;

    public class DataField
    {
        protected XmlElement element;

        internal DataField(XmlElement element)
        {
            element.ThrowIfNull<XmlElement>("element");
            this.element = element;
            try
            {
                this.GetDataFieldType();
            }
            catch (XmlException exception)
            {
                throw new ArgumentException("The element parameter is not a valid data-field.", exception);
            }
        }

        public DataField(DataFieldType type, string name = null, bool required = false, string label = null, string description = null)
        {
            this.element = Xml.Element("field", null);
            this.Type = new DataFieldType?(type);
            this.Name = name;
            this.Required = required;
            this.Label = label;
            this.Description = description;
        }

        protected void AssertType(DataFieldType expected)
        {
            DataFieldType? nullable = this.Type;
            DataFieldType type = expected;
            if ((((DataFieldType) nullable.GetValueOrDefault()) != type) || !nullable.HasValue)
            {
                throw new ArgumentException("The specified XML element is not a data-field of type '" + expected.ToString() + "'.");
            }
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

        private DataFieldType? GetDataFieldType()
        {
            DataFieldType? nullable;
            try
            {
                string attribute = this.element.GetAttribute("type");
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

        private void SetType(DataFieldType? type)
        {
            if (!type.HasValue)
            {
                this.element.RemoveAttribute("type");
            }
            else
            {
                string str = this.TypeToAttributeValue(type.Value);
                this.element.SetAttribute("type", str);
            }
        }

        public override string ToString()
        {
            return this.element.ToXmlString(false, false);
        }

        public XmlElement ToXmlElement()
        {
            return this.element;
        }

        private string TypeToAttributeValue(DataFieldType type)
        {
            StringBuilder builder = new StringBuilder();
            string s = type.ToString();
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsUpper(s, i) && (i > 0))
                {
                    builder.Append('-');
                }
                builder.Append(char.ToLower(s[i]));
            }
            return builder.ToString();
        }

        public string Description
        {
            get
            {
                if (this.element["desc"] != null)
                {
                    return this.element["desc"].InnerText;
                }
                return null;
            }
            private set
            {
                XmlElement oldChild = this.element["desc"];
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
                    this.element.Child(Xml.Element("desc", null).Text(value));
                }
            }
        }

        public string Label
        {
            get
            {
                string attribute = this.element.GetAttribute("label");
                return (string.IsNullOrEmpty(attribute) ? null : attribute);
            }
            private set
            {
                if (value == null)
                {
                    this.element.RemoveAttribute("label");
                }
                else
                {
                    this.element.SetAttribute("label", value);
                }
            }
        }

        public string Name
        {
            get
            {
                string attribute = this.element.GetAttribute("var");
                return (string.IsNullOrEmpty(attribute) ? null : attribute);
            }
            private set
            {
                if (value == null)
                {
                    this.element.RemoveAttribute("var");
                }
                else
                {
                    this.element.SetAttribute("var", value);
                }
            }
        }

        public bool Required
        {
            get
            {
                return (this.element["required"] != null);
            }
            private set
            {
                if (!value)
                {
                    if (this.element["required"] != null)
                    {
                        this.element.RemoveChild(this.element["required"]);
                    }
                }
                else if (this.element["required"] == null)
                {
                    this.element.Child(Xml.Element("required", null));
                }
            }
        }

        public DataFieldType? Type
        {
            get
            {
                return this.GetDataFieldType();
            }
            private set
            {
                this.SetType(value);
            }
        }

        public IEnumerable<string> Values
        {
            get
            {
                ISet<string> set = new HashSet<string>();
                foreach (XmlElement element in this.element.GetElementsByTagName("value"))
                {
                    set.Add(element.InnerText);
                }
                return set;
            }
        }
    }
}

