namespace S22.Xmpp
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    internal static class Xml
    {
        public static XmlElement Attr(this XmlElement e, string name, string value)
        {
            e.SetAttribute(name, value);
            return e;
        }

        public static XmlElement Child(this XmlElement e, XmlElement child)
        {
            XmlNode newChild = e.OwnerDocument.ImportNode(child, true);
            e.AppendChild(newChild);
            return e;
        }

        public static XmlElement Element(string name, string @namespace = null)
        {
            name.ThrowIfNullOrEmpty("name");
            return new XmlDocument().CreateElement(name, @namespace);
        }

        public static XmlElement Text(this XmlElement e, string text)
        {
            e.AppendChild(e.OwnerDocument.CreateTextNode(text));
            return e;
        }

        public static string ToXmlString(this XmlElement e, bool xmlDeclaration = false, bool leaveOpen = false)
        {
            StringBuilder builder = new StringBuilder("<" + e.Name);
            if (!string.IsNullOrEmpty(e.NamespaceURI))
            {
                builder.Append(" xmlns='" + e.NamespaceURI + "'");
            }
            foreach (XmlAttribute attribute in e.Attributes)
            {
                if ((attribute.Name != "xmlns") && (attribute.Value != null))
                {
                    builder.Append(" " + attribute.Name + "='" + SecurityElement.Escape(attribute.Value.ToString()) + "'");
                }
            }
            if (e.IsEmpty)
            {
                builder.Append("/>");
            }
            else
            {
                builder.Append(">");
                foreach (object obj2 in e.ChildNodes)
                {
                    if (obj2 is XmlElement)
                    {
                        builder.Append(((XmlElement) obj2).ToXmlString(false, false));
                    }
                    else if (obj2 is XmlText)
                    {
                        builder.Append(((XmlText) obj2).InnerText);
                    }
                }
                builder.Append("</" + e.Name + ">");
            }
            string input = builder.ToString();
            if (xmlDeclaration)
            {
                input = "<?xml version='1.0' encoding='UTF-8'?>" + input;
            }
            if (leaveOpen)
            {
                return Regex.Replace(input, "/>$", ">");
            }
            return input;
        }
    }
}

