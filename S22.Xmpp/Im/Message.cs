namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Xml;

    public class Message : S22.Xmpp.Core.Message
    {
        private DateTime timestamp;
        private MessageType type;

        internal Message(S22.Xmpp.Core.Message message) : base(null, null, null, null, null)
        {
            this.timestamp = DateTime.Now;
            message.ThrowIfNull<S22.Xmpp.Core.Message>("message");
            this.type = this.ParseType(message.Data.GetAttribute("type"));
            base.element = message.Data;
            this.AlternateSubjects = new XmlDictionary(base.element, "subject", "xml:lang");
            this.AlternateBodies = new XmlDictionary(base.element, "body", "xml:lang");
        }

        public Message(Jid to, string body = null, string subject = null, string thread = null, MessageType type = 0, CultureInfo language = null) : base(to, null, null, null, language)
        {
            this.timestamp = DateTime.Now;
            to.ThrowIfNull<Jid>("to");
            this.AlternateSubjects = new XmlDictionary(base.element, "subject", "xml:lang");
            this.AlternateBodies = new XmlDictionary(base.element, "body", "xml:lang");
            this.Type = type;
            this.Body = body;
            this.Subject = subject;
            this.Thread = thread;
        }

        public Message(Jid to, IDictionary<string, string> bodies, IDictionary<string, string> subjects = null, string thread = null, MessageType type = 0, CultureInfo language = null) : base(to, null, null, null, language)
        {
            this.timestamp = DateTime.Now;
            to.ThrowIfNull<Jid>("to");
            bodies.ThrowIfNull<IDictionary<string, string>>("bodies");
            this.AlternateSubjects = new XmlDictionary(base.element, "subject", "xml:lang");
            this.AlternateBodies = new XmlDictionary(base.element, "body", "xml:lang");
            this.Type = type;
            foreach (KeyValuePair<string, string> pair in bodies)
            {
                this.AlternateBodies.Add(pair.Key, pair.Value);
            }
            if (subjects != null)
            {
                foreach (KeyValuePair<string, string> pair in subjects)
                {
                    this.AlternateSubjects.Add(pair.Key, pair.Value);
                }
            }
            this.Thread = thread;
        }

        private XmlElement GetBare(string tag)
        {
            foreach (XmlElement element in base.element.GetElementsByTagName(tag))
            {
                if (string.IsNullOrEmpty(element.GetAttribute("xml:lang")))
                {
                    return element;
                }
            }
            return null;
        }

        private MessageType ParseType(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return MessageType.Normal;
            }
            return (MessageType) Enum.Parse(typeof(MessageType), value.Capitalize());
        }

        public IDictionary<string, string> AlternateBodies { get; private set; }

        public IDictionary<string, string> AlternateSubjects { get; private set; }

        public string Body
        {
            get
            {
                XmlElement bare = this.GetBare("body");
                if (bare != null)
                {
                    return bare.InnerText;
                }
                string str = this.AlternateBodies.Keys.FirstOrDefault<string>();
                return ((str != null) ? this.AlternateBodies[str] : null);
            }
            set
            {
                XmlElement bare = this.GetBare("body");
                if (bare != null)
                {
                    if (value == null)
                    {
                        base.element.RemoveChild(bare);
                    }
                    else
                    {
                        bare.InnerText = value;
                    }
                }
                else if (value != null)
                {
                    base.element.Child(Xml.Element("body", null).Text(value));
                }
            }
        }

        public string Subject
        {
            get
            {
                XmlElement bare = this.GetBare("subject");
                if (bare != null)
                {
                    return bare.InnerText;
                }
                string str = this.AlternateSubjects.Keys.FirstOrDefault<string>();
                return ((str != null) ? this.AlternateSubjects[str] : null);
            }
            set
            {
                XmlElement bare = this.GetBare("subject");
                if (bare != null)
                {
                    if (value == null)
                    {
                        base.element.RemoveChild(bare);
                    }
                    else
                    {
                        bare.InnerText = value;
                    }
                }
                else if (value != null)
                {
                    base.element.Child(Xml.Element("subject", null).Text(value));
                }
            }
        }

        public string Thread
        {
            get
            {
                if (base.element["thread"] != null)
                {
                    return base.element["thread"].InnerText;
                }
                return null;
            }
            set
            {
                XmlElement oldChild = base.element["thread"];
                if (oldChild != null)
                {
                    if (value == null)
                    {
                        base.element.RemoveChild(oldChild);
                    }
                    else
                    {
                        oldChild.InnerText = value;
                    }
                }
                else if (value != null)
                {
                    base.element.Child(Xml.Element("thread", null).Text(value));
                }
            }
        }

        public DateTime Timestamp
        {
            get
            {
                DateTime time;
                XmlElement element = base.element["delay"];
                if (((element != null) && (element.NamespaceURI == "urn:xmpp:delay")) && DateTime.TryParse(element.GetAttribute("stamp"), out time))
                {
                    return time;
                }
                return this.timestamp;
            }
        }

        public MessageType Type
        {
            get
            {
                return this.type;
            }
            set
            {
                this.type = value;
                string str = value.ToString().ToLowerInvariant();
                base.element.SetAttribute("type", str);
            }
        }
    }
}

