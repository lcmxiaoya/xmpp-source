namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Xml;

    internal class UserTune : XmppExtension
    {
        private Pep pep;

        public event EventHandler<TuneEventArgs> Tune;

        public UserTune(XmppIm im) : base(im)
        {
        }

        private string GetField(XmlElement tune, string name)
        {
            return ((tune[name] != null) ? tune[name].InnerText : null);
        }

        public override void Initialize()
        {
            this.pep = base.im.GetExtension<Pep>();
            this.pep.Subscribe("http://jabber.org/protocol/tune", new Action<Jid, XmlElement>(this.onTune));
        }

        private void onTune(Jid jid, XmlElement item)
        {
            if ((item != null) && (item["tune"] != null))
            {
                XmlElement tune = item["tune"];
                if (tune.IsEmpty)
                {
                    this.Tune.Raise<TuneEventArgs>(this, new TuneEventArgs(jid, null));
                }
                else
                {
                    int length = 0;
                    if (tune["length"] != null)
                    {
                        length = int.Parse(tune["length"].InnerText);
                    }
                    int rating = 0;
                    if (tune["rating"] != null)
                    {
                        rating = int.Parse(tune["rating"].InnerText);
                    }
                    TuneInformation information = new TuneInformation(this.GetField(tune, "title"), this.GetField(tune, "artist"), this.GetField(tune, "track"), length, rating, this.GetField(tune, "source"), this.GetField(tune, "uri"));
                    this.Tune.Raise<TuneEventArgs>(this, new TuneEventArgs(jid, information));
                }
            }
        }

        public void Publish(TuneInformation tune)
        {
            tune.ThrowIfNull<TuneInformation>("tune");
            this.Publish(tune.Title, tune.Artist, tune.Track, tune.Length, tune.Rating, tune.Source, tune.Uri);
        }

        public void Publish(string title = null, string artist = null, string track = null, int length = 0, int rating = 0, string source = null, string uri = null)
        {
            length.ThrowIfOutOfRange(0, 0x7fff);
            rating.ThrowIfOutOfRange(0, 10);
            XmlElement e = Xml.Element("tune", "http://jabber.org/protocol/tune");
            if (!string.IsNullOrEmpty(title))
            {
                e.Child(Xml.Element("title", null).Text(title));
            }
            if (!string.IsNullOrEmpty(artist))
            {
                e.Child(Xml.Element("artist", null).Text(artist));
            }
            if (!string.IsNullOrEmpty(track))
            {
                e.Child(Xml.Element("track", null).Text(track));
            }
            if (length > 0)
            {
                e.Child(Xml.Element("length", null).Text(length.ToString()));
            }
            if (rating > 0)
            {
                e.Child(Xml.Element("rating", null).Text(rating.ToString()));
            }
            if (!string.IsNullOrEmpty(source))
            {
                e.Child(Xml.Element("source", null).Text(source));
            }
            if (!string.IsNullOrEmpty(uri))
            {
                e.Child(Xml.Element("uri", null).Text(uri));
            }
            this.pep.Publish("http://jabber.org/protocol/tune", null, new XmlElement[] { e });
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/tune", "http://jabber.org/protocol/tune+notify" };
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
                return Extension.UserTune;
            }
        }
    }
}

