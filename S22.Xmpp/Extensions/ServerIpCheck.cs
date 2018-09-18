namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Xml;

    internal class ServerIpCheck : XmppExtension
    {
        private EntityCapabilities ecapa;

        public ServerIpCheck(XmppIm im) : base(im)
        {
        }

        public IPAddress GetExternalAddress()
        {
            IPAddress address;
            if (!this.ecapa.Supports(base.im.Jid.Domain, new Extension[] { Extension.ServerIpCheck }))
            {
                throw new NotSupportedException("The XMPP server does not support the 'Server IP Check' extension.");
            }
            Iq errorIq = base.im.IqRequest(IqType.Get, null, base.im.Jid, Xml.Element("address", "urn:xmpp:sic:1"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The external IP address could not be retrieved.");
            }
            XmlElement element = errorIq.Data["address"];
            if ((element == null) || (element["ip"] == null))
            {
                throw new XmppException("Erroneous IQ response.");
            }
            string innerText = element["ip"].InnerText;
            try
            {
                address = IPAddress.Parse(innerText);
            }
            catch (Exception exception)
            {
                throw new XmppException("The returned address is not a valid IP address.", exception);
            }
            return address;
        }

        public override void Initialize()
        {
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:sic:1" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.ServerIpCheck;
            }
        }
    }
}

