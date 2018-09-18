namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Xml;

    internal class EntityTime : XmppExtension, IInputFilter<Iq>
    {
        private EntityCapabilities ecapa;

        public EntityTime(XmppIm im) : base(im)
        {
        }

        public DateTime GetTime(Jid jid)
        {
            DateTime time2;
            jid.ThrowIfNull<Jid>("jid");
            if (!this.ecapa.Supports(jid, new Extension[] { Extension.EntityTime }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'Entity Time' extension.");
            }
            Iq errorIq = base.im.IqRequest(IqType.Get, jid, base.im.Jid, Xml.Element("time", "urn:xmpp:time"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The time could not be retrieved.");
            }
            XmlElement element = errorIq.Data["time"];
            if (((element == null) || (element["tzo"] == null)) || (element["utc"] == null))
            {
                throw new XmppException("Erroneous IQ response.");
            }
            string innerText = element["tzo"].InnerText;
            string s = element["utc"].InnerText;
            try
            {
                DateTime time = DateTime.Parse(s).ToUniversalTime();
                TimeSpan span = TimeSpan.Parse(innerText.TrimStart(new char[] { '+' }));
                time2 = time.Add(span);
            }
            catch (FormatException exception)
            {
                throw new XmppException("Invalid tzo or utc value.", exception);
            }
            return time2;
        }

        public override void Initialize()
        {
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
        }

        public bool Input(Iq stanza)
        {
            if (stanza.Type != IqType.Get)
            {
                return false;
            }
            XmlElement element = stanza.Data["time"];
            if ((element == null) || (element.NamespaceURI != "urn:xmpp:time"))
            {
                return false;
            }
            TimeSpan baseUtcOffset = TimeZoneInfo.Local.BaseUtcOffset;
            string text = ((baseUtcOffset < TimeSpan.Zero) ? "-" : "+") + baseUtcOffset.ToString(@"hh\:mm");
            string str2 = DateTime.UtcNow.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'''Z'");
            XmlElement data = Xml.Element("time", "urn:xmpp:time").Child(Xml.Element("tzo", null).Text(text)).Child(Xml.Element("utc", null).Text(str2));
            base.im.IqResult(stanza, data);
            return true;
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:time" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.EntityTime;
            }
        }
    }
}

