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

    internal class UserActivity : XmppExtension
    {
        private Pep pep;

        public event EventHandler<ActivityChangedEventArgs> ActivityChanged;

        public UserActivity(XmppIm im) : base(im)
        {
        }

        private string GeneralActivityToTagName(GeneralActivity activity)
        {
            StringBuilder builder = new StringBuilder();
            string s = activity.ToString();
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

        public override void Initialize()
        {
            this.pep = base.im.GetExtension<Pep>();
            this.pep.Subscribe("http://jabber.org/protocol/activity", new Action<Jid, XmlElement>(this.onActivity));
        }

        private void onActivity(Jid jid, XmlElement item)
        {
            if ((item != null) && (item["activity"] != null))
            {
                string str;
                XmlElement element = item["activity"];
                XmlElement element2 = null;
                GeneralActivity? nullable = null;
                if (element.IsEmpty)
                {
                    nullable = new GeneralActivity?(GeneralActivity.Undefined);
                }
                else
                {
                    foreach (object obj2 in Enum.GetValues(typeof(GeneralActivity)))
                    {
                        str = this.GeneralActivityToTagName((GeneralActivity) obj2);
                        if (element[str] != null)
                        {
                            nullable = new GeneralActivity?((GeneralActivity) obj2);
                            element2 = element[str];
                        }
                    }
                }
                SpecificActivity other = SpecificActivity.Other;
                if (element2 != null)
                {
                    foreach (object obj2 in Enum.GetValues(typeof(SpecificActivity)))
                    {
                        str = this.SpecificActivityToTagName((SpecificActivity) obj2);
                        if (element2[str] != null)
                        {
                            other = (SpecificActivity) obj2;
                        }
                    }
                }
                string description = (element["text"] != null) ? element["text"].InnerText : null;
                if (nullable.HasValue)
                {
                    this.ActivityChanged.Raise<ActivityChangedEventArgs>(this, new ActivityChangedEventArgs(jid, nullable.Value, other, description));
                }
            }
        }

        public void SetActivity(GeneralActivity activity, SpecificActivity specific = SpecificActivity.Other, string description = null)
        {
            XmlElement e = Xml.Element("activity", "http://jabber.org/protocol/activity");
            XmlElement element2 = Xml.Element(this.GeneralActivityToTagName(activity), null);
            if (specific != SpecificActivity.Other)
            {
                element2.Child(Xml.Element(this.SpecificActivityToTagName(specific), null));
            }
            e.Child(element2);
            if (description != null)
            {
                e.Child(Xml.Element("text", null).Text(description));
            }
            this.pep.Publish("http://jabber.org/protocol/activity", null, new XmlElement[] { e });
        }

        private string SpecificActivityToTagName(SpecificActivity activity)
        {
            StringBuilder builder = new StringBuilder();
            string s = activity.ToString();
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

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/activity", "http://jabber.org/protocol/activity+notify" };
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
                return Extension.UserActivity;
            }
        }
    }
}

