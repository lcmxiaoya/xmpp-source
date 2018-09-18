namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Extensions.Dataforms;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Xml;

    internal class StreamInitiation : XmppExtension, IInputFilter<Iq>
    {
        private EntityCapabilities ecapa;
        private IDictionary<string, Func<Jid, XmlElement, XmlElement>> profiles;

        public StreamInitiation(XmppIm im) : base(im)
        {
            this.profiles = new Dictionary<string, Func<Jid, XmlElement, XmlElement>>();
        }

        private XmlElement CreateFeatureElement(IEnumerable<string> streamOptions)
        {
            streamOptions.ThrowIfNull<IEnumerable<string>>("streamOptions");
            DataForm form = new RequestForm(null, null, new DataField[0]);
            HashSet<Option> options = new HashSet<Option>();
            foreach (string str in streamOptions)
            {
                options.Add(new Option(str, null));
            }
            form.Fields.Add(new ListField("stream-method", true, null, null, options, null));
            return FeatureNegotiation.Create(form);
        }

        private XmlElement CreateSiElement(string sid, string mimeType, string profile, IEnumerable<string> streamOptions, XmlElement data = null)
        {
            XmlElement child = this.CreateFeatureElement(streamOptions);
            XmlElement e = Xml.Element("si", "http://jabber.org/protocol/si").Attr("id", sid).Attr("mime-type", mimeType).Attr("profile", profile).Child(child);
            if (data != null)
            {
                e.Child(data);
            }
            return e;
        }

        private static string GenerateSessionId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 0x10);
        }

        public override void Initialize()
        {
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
        }

        public InitiationResult InitiateStream(Jid to, string mimeType, string profile, IEnumerable<string> streamOptions, XmlElement data = null)
        {
            to.ThrowIfNull<Jid>("to");
            mimeType.ThrowIfNull<string>("mimeType");
            profile.ThrowIfNull<string>("profile");
            streamOptions.ThrowIfNull<IEnumerable<string>>("streamOptions");
            if (streamOptions.Count<string>() == 0)
            {
                throw new ArgumentException("The streamOptions enumerable must include one or more stream-options.");
            }
            if (!this.ecapa.Supports(to, new Extension[] { Extension.StreamInitiation }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'Stream Initiation' extension.");
            }
            string sid = GenerateSessionId();
            XmlElement element = this.CreateSiElement(sid, mimeType, profile, streamOptions, data);
            Iq errorIq = base.im.IqRequest(IqType.Set, to, base.im.Jid, element, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "Stream initiation failed.");
            }
            XmlElement feature = errorIq.Data["si"]["feature"];
            return new InitiationResult(sid, this.ParseStreamMethod(feature), errorIq.Data["si"]);
        }

        public void InitiateStreamAsync(Jid to, string mimeType, string profile, IEnumerable<string> streamOptions, XmlElement data = null, Action<InitiationResult, Iq> cb = null)
        {
            to.ThrowIfNull<Jid>("to");
            mimeType.ThrowIfNull<string>("mimeType");
            profile.ThrowIfNull<string>("profile");
            streamOptions.ThrowIfNull<IEnumerable<string>>("streamOptions");
            if (streamOptions.Count<string>() == 0)
            {
                throw new ArgumentException("The streamOptions enumerable must include one or more stream-options.");
            }
            if (!this.ecapa.Supports(to, new Extension[] { Extension.StreamInitiation }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'Stream Initiation' extension.");
            }
            string sid = GenerateSessionId();
            XmlElement element = this.CreateSiElement(sid, mimeType, profile, streamOptions, data);
            base.im.IqRequestAsync(IqType.Set, to, base.im.Jid, element, null, delegate (string id, Iq iq) {
                CommonConfig.Logger.WriteInfo("文件握手阶段InitiateStreamAsync返回，响应结果：" + iq.ToString());
                if (cb != null)
                {
                    InitiationResult result = null;
                    if (iq.Type != IqType.Error)
                    {
                        XmlElement feature = iq.Data["si"]["feature"];
                        string method = this.ParseStreamMethod(feature);
                        result = new InitiationResult(sid, method, iq.Data["si"]);
                    }
                    cb(result, iq);
                }
            });
        }

        public bool Input(Iq stanza)
        {
            if (stanza.Type != IqType.Set)
            {
                return false;
            }
            XmlElement element = stanza.Data["si"];
            if ((element == null) || (element.NamespaceURI != "http://jabber.org/protocol/si"))
            {
                return false;
            }
            string attribute = element.GetAttribute("profile");
            if (!this.profiles.ContainsKey(attribute))
            {
                base.im.IqError(stanza, ErrorType.Cancel, ErrorCondition.BadRequest, "Unknown SI profile", new XmlElement[] { Xml.Element("bad-profile", "http://jabber.org/protocol/si") });
            }
            else
            {
                try
                {
                    XmlElement data = this.profiles[attribute](stanza.From, stanza.Data["si"]);
                    base.im.IqResponse((data.Name == "error") ? IqType.Error : IqType.Result, stanza.Id, stanza.From, base.im.Jid, data, null);
                }
                catch (Exception)
                {
                    base.im.IqError(stanza, ErrorType.Cancel, ErrorCondition.ServiceUnavailable, null, new XmlElement[0]);
                }
            }
            return true;
        }

        private string ParseStreamMethod(XmlElement feature)
        {
            feature.ThrowIfNull<XmlElement>("feature");
            DataField field = FeatureNegotiation.Parse(feature).Fields["stream-method"];
            if (field == null)
            {
                throw new ArgumentException("Missing or erroneous 'stream-method' field.");
            }
            string str = field.Values.FirstOrDefault<string>();
            if (str == null)
            {
                throw new ArgumentException("No stream-method selected.");
            }
            return str;
        }

        public void RegisterProfile(string name, Func<Jid, XmlElement, XmlElement> cb)
        {
            name.ThrowIfNull<string>("name");
            cb.ThrowIfNull<Func<Jid, XmlElement, XmlElement>>("cb");
            this.profiles.Add(name, cb);
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/si" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.StreamInitiation;
            }
        }
    }
}

