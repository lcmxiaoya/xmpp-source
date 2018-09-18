namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Extensions.Dataforms;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

    internal class InBandRegistration : XmppExtension
    {
        private BitsOfBinary bob;
        private EntityCapabilities ecapa;

        public InBandRegistration(XmppIm im) : base(im)
        {
        }

        public void ChangePassword(string newPassword)
        {
            newPassword.ThrowIfNull<string>("newPassword");
            Iq errorIq = base.im.IqRequest(IqType.Set, null, null, Xml.Element("query", "jabber:iq:register").Child(Xml.Element("username", null).Text(base.im.Username)).Child(Xml.Element("password", null).Text(newPassword)), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The password could not be changed.");
            }
        }

        private RequestForm CreateDataForm(XmlElement query)
        {
            string instructions = (query["instructions"] != null) ? query["instructions"].InnerText : null;
            RequestForm form = new RequestForm(null, instructions, new DataField[0]);
            foreach (XmlElement element in query.ChildNodes)
            {
                if (!(element.Name == "instructions"))
                {
                    form.Fields.Add(new TextField(element.Name, true, null, null, null));
                }
            }
            return form;
        }

        public override void Initialize()
        {
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
            this.bob = base.im.GetExtension<BitsOfBinary>();
        }

        public void Register(RegistrationCallback callback)
        {
            callback.ThrowIfNull<RegistrationCallback>("callback");
            Iq errorIq = base.im.IqRequest(IqType.Get, null, null, Xml.Element("query", "jabber:iq:register"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw new NotSupportedException("The XMPP server does not support the 'In-Band Registration' extension.");
            }
            XmlElement query = errorIq.Data["query"];
            if ((query == null) || (query.NamespaceURI != "jabber:iq:register"))
            {
                throw new XmppException("Erroneous server response: " + errorIq);
            }
            if (query["registered"] != null)
            {
                throw new XmppException("The XMPP entity is already registered.");
            }
            XmlElement element2 = query["data"];
            if ((element2 != null) && (element2.NamespaceURI == "urn:xmpp:bob"))
            {
                BobData bob = BobData.Parse(element2);
                this.bob.Add(bob);
            }
            RequestForm form = null;
            bool flag = query["x"] != null;
            if (flag)
            {
                form = DataFormFactory.Create(query["x"]) as RequestForm;
            }
            else
            {
                form = this.CreateDataForm(query);
            }
            SubmitForm form2 = callback(form);
            XmlElement e = Xml.Element("query", "jabber:iq:register");
            if (flag)
            {
                e.Child(form2.ToXmlElement());
            }
            else
            {
                foreach (DataField field in form2.Fields)
                {
                    e.Child(Xml.Element(field.Name, null).Text(field.Values.FirstOrDefault<string>()));
                }
            }
            errorIq = base.im.IqRequest(IqType.Set, null, null, e, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The registration could not be completed.");
            }
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "jabber:iq:register" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.InBandRegistration;
            }
        }
    }
}

