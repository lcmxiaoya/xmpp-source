namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Xml;

    internal class BitsOfBinary : XmppExtension, IInputFilter<Iq>
    {
        private IDictionary<string, BobData> cache;
        private EntityCapabilities ecapa;

        public BitsOfBinary(XmppIm im) : base(im)
        {
            this.cache = new Dictionary<string, BobData>();
        }

        public void Add(BobData bob)
        {
            bob.ThrowIfNull<BobData>("bob");
            this.cache[bob.Cid] = bob;
        }

        public BobData Create(byte[] data, string type, bool cache = true)
        {
            data.ThrowIfNull<byte[]>("data");
            type.ThrowIfNull<string>("type");
            BobData data2 = new BobData(data, type);
            if (cache)
            {
                this.cache[data2.Cid] = data2;
            }
            return data2;
        }

        public BobData Get(string cid)
        {
            cid.ThrowIfNull<string>("cid");
            if (!this.cache.ContainsKey(cid))
            {
                throw new ArgumentException("A data-item with the specified CID does not exist.");
            }
            return this.cache[cid];
        }

        public BobData Get(string cid, Jid from, bool cache = true)
        {
            BobData data2;
            cid.ThrowIfNull<string>("cid");
            from.ThrowIfNull<Jid>("from");
            if (this.cache.ContainsKey(cid))
            {
                return this.cache[cid];
            }
            if (!this.ecapa.Supports(from, new Extension[] { Extension.BitsOfBinary }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'Bits of Binary' extension.");
            }
            Iq errorIq = base.im.IqRequest(IqType.Get, from, base.im.Jid, Xml.Element("data", "urn:xmpp:bob").Attr("cid", cid), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The data-item with the specified CID could not be retrieved.");
            }
            XmlElement element = errorIq.Data["data"];
            if ((element == null) || (element.NamespaceURI != "urn:xmpp:bob"))
            {
                throw new XmppException("Erroneous response.");
            }
            try
            {
                BobData data = BobData.Parse(element);
                if (cache)
                {
                    this.cache[cid] = data;
                }
                data2 = data;
            }
            catch (ArgumentException exception)
            {
                throw new XmppException("The retrieved data-item could not be processed.", exception);
            }
            return data2;
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
            XmlElement element = stanza.Data["data"];
            if ((element == null) || (element.NamespaceURI != "urn:xmpp:bob"))
            {
                return false;
            }
            string attribute = element.GetAttribute("cid");
            if (this.cache.ContainsKey(attribute))
            {
                BobData data = this.cache[attribute];
                XmlElement element2 = Xml.Element("data", "urn:xmpp:bob").Attr("cid", attribute).Attr("type", data.Type).Text(Convert.ToBase64String(data.Data));
                base.im.IqResult(stanza, element2);
            }
            else
            {
                base.im.IqError(stanza, ErrorType.Cancel, ErrorCondition.ItemNotFound, null, new XmlElement[0]);
            }
            return true;
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:bob" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.BitsOfBinary;
            }
        }
    }
}

