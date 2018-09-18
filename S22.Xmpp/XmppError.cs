namespace S22.Xmpp
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;

    public class XmppError
    {
        private ErrorCondition condition;
        private ErrorType type;

        internal XmppError(XmlElement error)
        {
            error.ThrowIfNull<XmlElement>("error");
            ErrorType type = (ErrorType) Enum.Parse(typeof(ErrorType), error.GetAttribute("type"), true);
            ErrorCondition? nullable = null;
            foreach (object obj2 in Enum.GetValues(typeof(ErrorCondition)))
            {
                string str = this.ErrorConditionToTagName((ErrorCondition) obj2);
                if (error[str] != null)
                {
                    nullable = new ErrorCondition?((ErrorCondition) obj2);
                }
            }
            if (!nullable.HasValue)
            {
                throw new ArgumentException("The error XML element does not contain a valid XMPP error condition element.");
            }
            this.Data = error;
            this.Type = type;
            this.Condition = nullable.Value;
        }

        public XmppError(ErrorType type, ErrorCondition condition, params XmlElement[] data) : this(type, condition, null, data)
        {
        }

        internal XmppError(ErrorType type, ErrorCondition condition, string text = null, params XmlElement[] data)
        {
            this.Data = Xml.Element("error", null);
            this.Type = type;
            this.Condition = condition;
            this.Text = text;
            if (data != null)
            {
                foreach (XmlElement element in data)
                {
                    if (element != null)
                    {
                        this.Data.Child(element);
                    }
                }
            }
        }

        private string ErrorConditionToTagName(ErrorCondition condition)
        {
            StringBuilder builder = new StringBuilder();
            string s = condition.ToString();
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

        private void SetCondition(ErrorCondition condition)
        {
            ISet<XmlElement> set = new HashSet<XmlElement>();
            foreach (object obj2 in Enum.GetValues(typeof(ErrorCondition)))
            {
                string str = this.ErrorConditionToTagName((ErrorCondition) obj2);
                if (this.Data[str] != null)
                {
                    set.Add(this.Data[str]);
                }
            }
            foreach (XmlElement element in set)
            {
                this.Data.RemoveChild(element);
            }
            string name = this.ErrorConditionToTagName(condition);
            this.Data.Child(Xml.Element(name, "urn:ietf:params:xml:ns:xmpp-stanzas"));
            this.condition = condition;
        }

        private ErrorCondition TagNameToErrorCondition(string tagName)
        {
            tagName.ThrowIfNull<string>("tagName");
            Array values = Enum.GetValues(typeof(ErrorCondition));
            foreach (object obj2 in values)
            {
                if (this.ErrorConditionToTagName((ErrorCondition) obj2) == tagName)
                {
                    return (ErrorCondition) obj2;
                }
            }
            throw new ArgumentException("The specified tag name is not a valid XMPP error condition.");
        }

        public override string ToString()
        {
            return this.Data.ToXmlString(false, false);
        }

        public ErrorCondition Condition
        {
            get
            {
                return this.condition;
            }
            set
            {
                this.SetCondition(value);
            }
        }

        public XmlElement Data { get; private set; }

        public string Text
        {
            get
            {
                XmlElement element = this.Data["text"];
                if (element != null)
                {
                    return (string.IsNullOrEmpty(element.InnerText) ? null : element.InnerText);
                }
                return null;
            }
            set
            {
                XmlElement oldChild = this.Data["text"];
                if (oldChild != null)
                {
                    if (value == null)
                    {
                        this.Data.RemoveChild(oldChild);
                    }
                    else
                    {
                        oldChild.InnerText = value;
                    }
                }
                else if (value != null)
                {
                    this.Data.Child(Xml.Element("text", "urn:ietf:params:xml:ns:xmpp-stanzas").Text(value));
                }
            }
        }

        public ErrorType Type
        {
            get
            {
                return this.type;
            }
            set
            {
                this.type = value;
                this.Data.SetAttribute("type", value.ToString().ToLowerInvariant());
            }
        }
    }
}

