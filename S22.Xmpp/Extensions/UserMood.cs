namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Xml;

    internal class UserMood : XmppExtension
    {
        private Pep pep;

        public event EventHandler<MoodChangedEventArgs> MoodChanged;

        public UserMood(XmppIm im) : base(im)
        {
        }

        public override void Initialize()
        {
            this.pep = base.im.GetExtension<Pep>();
            this.pep.Subscribe("http://jabber.org/protocol/mood", new Action<Jid, XmlElement>(this.onMood));
        }

        private string MoodToTagName(Mood mood)
        {
            StringBuilder builder = new StringBuilder();
            string s = mood.ToString();
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsUpper(s, i) && (i > 0))
                {
                    builder.Append('_');
                }
                builder.Append(char.ToLower(s[i]));
            }
            return builder.ToString();
        }

        private void onMood(Jid jid, XmlElement item)
        {
            if ((item != null) && (item["mood"] != null))
            {
                XmlElement element = item["mood"];
                Mood? nullable = null;
                if (element.IsEmpty)
                {
                    nullable = new Mood?(Mood.Undefined);
                }
                else
                {
                    foreach (object obj2 in Enum.GetValues(typeof(Mood)))
                    {
                        string str = this.MoodToTagName((Mood) obj2);
                        if (element[str] != null)
                        {
                            nullable = new Mood?((Mood) obj2);
                        }
                    }
                }
                string description = (element["text"] != null) ? element["text"].InnerText : null;
                if (nullable.HasValue)
                {
                    this.MoodChanged.Raise<MoodChangedEventArgs>(this, new MoodChangedEventArgs(jid, nullable.Value, description));
                }
            }
        }

        public void SetMood(Mood mood, string description = null)
        {
            XmlElement e = Xml.Element("mood", "http://jabber.org/protocol/mood").Child(Xml.Element(this.MoodToTagName(mood), null));
            if (description != null)
            {
                e.Child(Xml.Element("text", null).Text(description));
            }
            this.pep.Publish("http://jabber.org/protocol/mood", null, new XmlElement[] { e });
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/mood", "http://jabber.org/protocol/mood+notify" };
            }
        }

        public bool Supported
        {
            get
            {
                return this.pep.Supported;
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.UserMood;
            }
        }
    }
}

