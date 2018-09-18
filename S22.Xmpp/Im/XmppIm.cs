namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Xml;

    public class XmppIm : IDisposable
    {
        private DateTime _lastCheckRosterTime;
        private XmppCore core;
        private bool disposed;
        private ISet<XmppExtension> extensions;

        public event EventHandler<S22.Xmpp.Im.ErrorEventArgs> Error;

        public event EventHandler<S22.Xmpp.Im.IqEventArgs> IqRequestEvents;

        public event EventHandler<S22.Xmpp.Im.IqEventArgs> IqResponseEvents;

        public event EventHandler<S22.Xmpp.Im.MessageEventArgs> Message;

        public event EventHandler<RosterUpdatedEventArgs> RosterUpdated;

        public event EventHandler<StatusEventArgs> Status;

        public event EventHandler<SubscriptionApprovedEventArgs> SubscriptionApproved;

        public event EventHandler<SubscriptionRefusedEventArgs> SubscriptionRefused;

        public event EventHandler<UnsubscribedEventArgs> Unsubscribed;

        public XmppIm(string hostname, int port = 0x1466, bool tls = true, RemoteCertificateValidationCallback validate = null)
        {
            this.extensions = new HashSet<XmppExtension>();
            this._lastCheckRosterTime = DateTime.Now.AddSeconds(60.0);
            this.core = new XmppCore(hostname, port, tls, validate);
            this.SetupEventHandlers();
        }

        public XmppIm(string hostname, string username, string password, int port = 0x1466, bool tls = true, RemoteCertificateValidationCallback validate = null)
        {
            this.extensions = new HashSet<XmppExtension>();
            this._lastCheckRosterTime = DateTime.Now.AddSeconds(60.0);
            this.core = new XmppCore(hostname, username, password, port, tls, validate);
            this.SetupEventHandlers();
        }

        public void AddToRoster(RosterItem item)
        {
            this.AssertValid(true);
            item.ThrowIfNull<RosterItem>("item");
            XmlElement e = Xml.Element("item", null).Attr("jid", item.Jid.ToString());
            if (!string.IsNullOrEmpty(item.Name))
            {
                e.Attr("name", item.Name);
            }
            foreach (string str in item.Groups)
            {
                e.Child(Xml.Element("group", null).Text(str));
            }
            XmlElement data = Xml.Element("query", "jabber:iq:roster").Child(e);
            Iq errorIq = this.IqRequest(IqType.Set, null, this.Jid, data, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The item could not be added to the roster.");
            }
        }

        public void ApproveSubscriptionRequest(S22.Xmpp.Jid jid)
        {
            this.AssertValid(true);
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            S22.Xmpp.Im.Presence presence = new S22.Xmpp.Im.Presence(jid, null, PresenceType.Subscribed, null, null, new XmlElement[0]);
            this.SendPresence(presence);
        }

        private void AssertValid(bool authRequired = true)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            if (!this.Connected)
            {
                throw new InvalidOperationException("Not connected to XMPP server.");
            }
            if (!(!authRequired || this.Authenticated))
            {
                throw new InvalidOperationException("Not authenticated with XMPP server.");
            }
        }

        public void Autenticate(string username, string password)
        {
            username.ThrowIfNull<string>("username");
            password.ThrowIfNull<string>("password");
            this.core.Authenticate(username, password);
            this.EstablishSession();
            Roster roster = this.GetRoster();
            this.SendPresence(new S22.Xmpp.Im.Presence(null, null, PresenceType.Available, null, null, new XmlElement[0]));
        }

        public void Close()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            this.Dispose();
        }

        public Roster Connect(string resource = null)
        {
            Roster roster2;
            CommonConfig.Logger.WriteInfo("开始连接...");
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            foreach (XmppExtension extension in this.extensions)
            {
                try
                {
                    extension.Initialize();
                }
                catch (Exception exception)
                {
                    CommonConfig.Logger.WriteError(exception);
                    throw new XmppException("Initialization of " + extension.Xep + " failed.", exception);
                }
            }
            try
            {
                this.core.Connect(resource);
                this.IsHasRosterOnline = true;
                if (this.Username == null)
                {
                    return null;
                }
                this.EstablishSession();
                Roster roster = this.GetRoster();
                this.SendPresence(new S22.Xmpp.Im.Presence(null, null, PresenceType.Available, null, null, new XmlElement[0]));
                roster2 = roster;
            }
            catch (SocketException exception2)
            {
                CommonConfig.Logger.WriteError(exception2);
                throw new IOException("Could not connect to the server", exception2);
            }
            return roster2;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    if (this.core != null)
                    {
                        this.core.Close();
                    }
                    this.core = null;
                }
            }
        }

        public void EditPrivacyList(PrivacyList list)
        {
            this.AssertValid(true);
            list.ThrowIfNull<PrivacyList>("list");
            if (list.Count == 0)
            {
                throw new ArgumentException("The list must contain one or more privacy rules.");
            }
            XmlElement e = Xml.Element("list", null).Attr("name", list.Name);
            foreach (PrivacyRule rule in list)
            {
                XmlElement element2 = Xml.Element("item", null).Attr("action", (rule.Allow ? "allow" : "deny")).Attr("order", rule.Order.ToString());
                if (rule.Granularity.HasFlag(PrivacyGranularity.Message))
                {
                    element2.Child(Xml.Element("message", null));
                }
                if (rule.Granularity.HasFlag(PrivacyGranularity.Iq))
                {
                    element2.Child(Xml.Element("iq", null));
                }
                if (rule.Granularity.HasFlag(PrivacyGranularity.PresenceIn))
                {
                    element2.Child(Xml.Element("presence-in", null));
                }
                if (rule.Granularity.HasFlag(PrivacyGranularity.PresenceOut))
                {
                    element2.Child(Xml.Element("presence-out", null));
                }
                if (rule is JidPrivacyRule)
                {
                    JidPrivacyRule rule2 = rule as JidPrivacyRule;
                    element2.Attr("type", "jid");
                    element2.Attr("value", rule2.Jid.ToString());
                }
                else if (rule is GroupPrivacyRule)
                {
                    GroupPrivacyRule rule3 = rule as GroupPrivacyRule;
                    element2.Attr("type", "group");
                    element2.Attr("value", rule3.Group);
                }
                else if (rule is SubscriptionPrivacyRule)
                {
                    SubscriptionPrivacyRule rule4 = rule as SubscriptionPrivacyRule;
                    element2.Attr("type", "subscription");
                    element2.Attr("value", rule4.SubscriptionState.ToString().ToLowerInvariant());
                }
                e.Child(element2);
            }
            Iq errorIq = this.IqRequest(IqType.Set, null, this.Jid, Xml.Element("query", "jabber:iq:privacy").Child(e), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The privacy list could not be edited.");
            }
        }

        private void EstablishSession()
        {
            Iq errorIq = this.IqRequest(IqType.Set, this.Hostname, null, Xml.Element("session", "urn:ietf:params:xml:ns:xmpp-session"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "Session establishment failed.");
            }
        }

        public string GetActivePrivacyList()
        {
            this.AssertValid(true);
            Iq errorIq = this.IqRequest(IqType.Get, null, this.Jid, Xml.Element("query", "jabber:iq:privacy"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The privacy list could not be retrieved.");
            }
            XmlElement element = errorIq.Data["query"];
            if ((element == null) || (element.NamespaceURI != "jabber:iq:privacy"))
            {
                throw new XmppException("Erroneous server response: " + errorIq);
            }
            XmlElement element2 = element["active"];
            if (element2 == null)
            {
                return null;
            }
            string attribute = element2.GetAttribute("name");
            if (string.IsNullOrEmpty(attribute))
            {
                return null;
            }
            return attribute;
        }

        public string GetDefaultPrivacyList()
        {
            this.AssertValid(true);
            Iq errorIq = this.IqRequest(IqType.Get, null, this.Jid, Xml.Element("query", "jabber:iq:privacy"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The privacy list could not be retrieved.");
            }
            XmlElement element = errorIq.Data["query"];
            if ((element == null) || (element.NamespaceURI != "jabber:iq:privacy"))
            {
                throw new XmppException("Erroneous server response: " + errorIq);
            }
            XmlElement element2 = element["default"];
            if (element2 == null)
            {
                return null;
            }
            string attribute = element2.GetAttribute("name");
            if (string.IsNullOrEmpty(attribute))
            {
                return null;
            }
            return attribute;
        }

        internal T GetExtension<T>() where T: XmppExtension
        {
            foreach (XmppExtension extension in this.extensions)
            {
                if (extension.GetType() == typeof(T))
                {
                    return (T) extension;
                }
            }
            return default(T);
        }

        internal XmppExtension GetExtension(string @namespace)
        {
            @namespace.ThrowIfNull<string>("namespace");
            foreach (XmppExtension extension in this.extensions)
            {
                if (extension.Namespaces.Contains<string>(@namespace))
                {
                    return extension;
                }
            }
            return null;
        }

        internal XmppExtension GetExtension(Type type)
        {
            type.ThrowIfNull<Type>("type");
            foreach (XmppExtension extension in this.extensions)
            {
                if (extension.GetType() == type)
                {
                    return extension;
                }
            }
            return null;
        }

        public PrivacyList GetPrivacyList(string name)
        {
            this.AssertValid(true);
            name.ThrowIfNull<string>("name");
            XmlElement data = Xml.Element("query", "jabber:iq:privacy").Child(Xml.Element("list", null).Attr("name", name));
            Iq errorIq = this.IqRequest(IqType.Get, null, this.Jid, data, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The privacy list could not be retrieved.");
            }
            data = errorIq.Data["query"];
            if (((data == null) || (data.NamespaceURI != "jabber:iq:privacy")) || (data["list"] == null))
            {
                throw new XmppException("Erroneous server response: " + errorIq);
            }
            PrivacyList list = new PrivacyList(name);
            XmlElement element2 = data["list"];
            foreach (XmlElement element3 in element2.GetElementsByTagName("item"))
            {
                try
                {
                    PrivacyRule item = this.ParsePrivacyItem(element3);
                    list.Add(item);
                }
                catch (Exception exception)
                {
                    throw new XmppException("Erroneous privacy rule.", exception);
                }
            }
            return list;
        }

        public IEnumerable<PrivacyList> GetPrivacyLists()
        {
            this.AssertValid(true);
            Iq errorIq = this.IqRequest(IqType.Get, null, this.Jid, Xml.Element("query", "jabber:iq:privacy"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                Util.ExceptionFromError(errorIq, "The privacy lists could not be retrieved.");
            }
            XmlElement element = errorIq.Data["query"];
            if ((element == null) || (element.NamespaceURI != "jabber:iq:privacy"))
            {
                throw new XmppException("Erroneous server response: " + errorIq);
            }
            ISet<PrivacyList> set = new HashSet<PrivacyList>();
            foreach (XmlElement element2 in element.GetElementsByTagName("list"))
            {
                string attribute = element2.GetAttribute("name");
                if (!string.IsNullOrEmpty(attribute))
                {
                    set.Add(this.GetPrivacyList(attribute));
                }
            }
            return set;
        }

        public Roster GetRoster()
        {
            this.AssertValid(true);
            Iq errorIq = this.IqRequest(IqType.Get, null, this.Jid, Xml.Element("query", "jabber:iq:roster"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The roster could not be retrieved.");
            }
            XmlElement element = errorIq.Data["query"];
            if ((element == null) || (element.NamespaceURI != "jabber:iq:roster"))
            {
                throw new XmppException("Erroneous server response.");
            }
            return this.ParseRoster(errorIq.Data);
        }

        internal void IqError(Iq iq, ErrorType type, ErrorCondition condition, string text = null, params XmlElement[] data)
        {
            this.AssertValid(false);
            iq.ThrowIfNull<Iq>("iq");
            Iq response = new Iq(IqType.Error, iq.Id, iq.From, this.Jid, new XmppError(type, condition, text, data).Data, null);
            this.core.IqResponse(response);
        }

        internal Iq IqRequest(IqType type, S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, XmlElement data = null, CultureInfo language = null, int millisecondsTimeout = -1, string seqID = "")
        {
            //Iq stanza = new Iq(type, null, to, from, data, language);  
            //foreach (XmppExtension extension in this.extensions)
            //{
            //    IOutputFilter<Iq> filter = extension as IOutputFilter<Iq>;
            //    if (filter != null)
            //    {
            //        filter.Output(stanza);
            //    }
            //}
            //return this.core.IqRequest(stanza, millisecondsTimeout, seqID);

            Iq iq = new Iq(type, null, to, from, data, language);
            // Invoke IOutput<Iq> Plugins.
            foreach (var ext in extensions)
            {
                var filter = ext as IOutputFilter<Iq>;
                if (filter != null)
                    filter.Output(iq);
            }
            return core.IqRequest(iq, millisecondsTimeout, seqID);
        }

        internal string IqRequestAsync(IqType type, S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, XmlElement data = null, CultureInfo language = null, Action<string, Iq> callback = null)
        {
            Iq stanza = new Iq(type, null, to, from, data, language);
            foreach (XmppExtension extension in this.extensions)
            {
                IOutputFilter<Iq> filter = extension as IOutputFilter<Iq>;
                if (filter != null)
                {
                    filter.Output(stanza);
                }
            }
            return this.core.IqRequestAsync(stanza, callback);
        }

        public void IqRequestBeatJieShun(S22.Xmpp.Jid to, S22.Xmpp.Jid from, IqType iqType =  IqType.Get)
        {
            XmlElement data = Xml.Element("b", "urn:xmpp:beat");
            this.IqRequest(iqType, to, from, data, null, -1, "");
        }

        public Iq IqRequestJieShun(S22.Xmpp.Jid to, S22.Xmpp.Jid from, string cnt, int crc, string mode, IqType iqType = 0, int millisecondsTimeout = -1, string seqID = "")
        {
            XmlElement e = Xml.Element("dreq", "xmpp:iq:shunt");
            e.Child(Xml.Element("cnt", null).Text(XmlHelper.FilterChar(cnt)));
            e.Child(Xml.Element("crc", null).Text(crc.ToString()));
            e.Child(Xml.Element("mode", null).Text(mode));
            return this.IqRequest(iqType, to, from, e, null, millisecondsTimeout, seqID);
        }

        internal void IqResponse(IqType type, string id, S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, XmlElement data = null, CultureInfo language = null)
        {
            this.AssertValid(false);
            Iq stanza = new Iq(type, id, to, from, data, language);
            foreach (XmppExtension extension in this.extensions)
            {
                IOutputFilter<Iq> filter = extension as IOutputFilter<Iq>;
                if (filter != null)
                {
                    filter.Output(stanza);
                }
            }
            this.core.IqResponse(stanza);
        }

        internal void IqResponseJieShun(IqType type, string id, string cnt, int crc, int rc, string sync, S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, CultureInfo language = null)
        {
            this.AssertValid(false);
            XmlElement e = Xml.Element("dres", "xmpp:iq:shunt");
            e.Child(Xml.Element("cnt", null).Text(XmlHelper.FilterChar(cnt)));
            e.Child(Xml.Element("crc", null).Text(crc.ToString()));
            e.Child(Xml.Element("sync", null).Text(sync));
            e.Child(Xml.Element("rc", null).Text("0"));
            Iq stanza = new Iq(type, id, to, from, e, language);
            foreach (XmppExtension extension in this.extensions)
            {
                IOutputFilter<Iq> filter = extension as IOutputFilter<Iq>;
                if (filter != null)
                {
                    filter.Output(stanza);
                }
            }
            this.core.IqResponse(stanza);
        }

        internal void IqResult(Iq iq, XmlElement data = null)
        {
            this.AssertValid(false);
            iq.ThrowIfNull<Iq>("iq");
            Iq response = new Iq(IqType.Result, iq.Id, iq.From, this.Jid, data, null);
            this.core.IqResponse(response);
        }

        internal T LoadExtension<T>() where T: XmppExtension
        {
            XmppExtension item = Activator.CreateInstance(typeof(T), new object[] { this }) as XmppExtension;
            this.extensions.Add(item);
            return (T) item;
        }

        private void OnIq(Iq iq)
        {
            foreach (XmppExtension extension in this.extensions)
            {
                IInputFilter<Iq> filter = extension as IInputFilter<Iq>;
                if ((filter != null) && filter.Input(iq))
                {
                    return;
                }
            }
            XmlElement element = iq.Data["query"];
            XmlElement element2 = iq.Data["b"];
            if (element != null)
            {
                string namespaceURI = element.NamespaceURI;
                if ((namespaceURI != null) && (namespaceURI == "jabber:iq:roster"))
                {
                    CommonConfig.Logger.WriteInfo(iq.ToString());
                    this.ProcessRosterIq(iq);
                    return;
                }
            }
            else if (element2 != null)
            {
                CommonConfig.Logger.WriteInfo("收到捷顺beat心跳包");
                this.ProcessBeatIq(iq);
            }
            else
            {
                if (iq.Data["dreq"] != null)
                {
                    this.IqRequestEvents.Raise<S22.Xmpp.Im.IqEventArgs>(this, new S22.Xmpp.Im.IqEventArgs(iq.From, iq));
                    return;
                }
                if (iq.Data["dres"] != null)
                {
                    this.IqResponseEvents.Raise<S22.Xmpp.Im.IqEventArgs>(this, new S22.Xmpp.Im.IqEventArgs(iq.From, iq));
                    return;
                }
            }
            this.IqError(iq, ErrorType.Cancel, ErrorCondition.FeatureNotImplemented, null, new XmlElement[0]);
        }

        private void OnMessage(S22.Xmpp.Im.Message message)
        {
            foreach (XmppExtension extension in this.extensions)
            {
                IInputFilter<S22.Xmpp.Im.Message> filter = extension as IInputFilter<S22.Xmpp.Im.Message>;
                if ((filter != null) && filter.Input(message))
                {
                    return;
                }
            }
            if (message.Data["body"] != null)
            {
                this.Message.Raise<S22.Xmpp.Im.MessageEventArgs>(this, new S22.Xmpp.Im.MessageEventArgs(message.From, message));
            }
        }

        private void OnPresence(S22.Xmpp.Im.Presence presence)
        {
            this.IsHasRosterOnline = true;
            foreach (XmppExtension extension in this.extensions)
            {
                IInputFilter<S22.Xmpp.Im.Presence> filter = extension as IInputFilter<S22.Xmpp.Im.Presence>;
                if ((filter != null) && filter.Input(presence))
                {
                    return;
                }
            }
            switch (presence.Type)
            {
                case PresenceType.Available:
                case PresenceType.Unavailable:
                    this.ProcessStatusNotification(presence);
                    break;

                case PresenceType.Subscribe:
                    this.ProcessSubscriptionRequest(presence);
                    break;

                case PresenceType.Subscribed:
                case PresenceType.Unsubscribed:
                    this.ProcessSubscriptionResult(presence);
                    break;

                case PresenceType.Unsubscribe:
                    this.ProcessUnsubscribeRequest(presence);
                    break;
            }
        }

        private PrivacyRule ParsePrivacyItem(XmlElement item)
        {
            item.ThrowIfNull<XmlElement>("item");
            bool allow = item.GetAttribute("action") == "allow";
            uint order = uint.Parse(item.GetAttribute("order"));
            PrivacyGranularity granularity = 0;
            if (item["message"] != null)
            {
                granularity |= PrivacyGranularity.Message;
            }
            if (item["iq"] != null)
            {
                granularity |= PrivacyGranularity.Iq;
            }
            if (item["presence-in"] != null)
            {
                granularity |= PrivacyGranularity.PresenceIn;
            }
            if (item["presence-out"] != null)
            {
                granularity |= PrivacyGranularity.PresenceOut;
            }
            string attribute = item.GetAttribute("type");
            string str2 = item.GetAttribute("value");
            Dictionary<string, SubscriptionState> dictionary2 = new Dictionary<string, SubscriptionState>();
            dictionary2.Add("none", SubscriptionState.None);
            dictionary2.Add("to", SubscriptionState.To);
            dictionary2.Add("from", SubscriptionState.From);
            dictionary2.Add("both", SubscriptionState.Both);
            Dictionary<string, SubscriptionState> dictionary = dictionary2;
            if (string.IsNullOrEmpty(attribute))
            {
                return new PrivacyRule(allow, order, granularity);
            }
            if (string.IsNullOrEmpty(str2))
            {
                throw new ArgumentException("Missing value attribute.");
            }
            switch (attribute)
            {
                case "jid":
                    return new JidPrivacyRule(new S22.Xmpp.Jid(str2), allow, order, granularity);

                case "group":
                    return new GroupPrivacyRule(str2, allow, order, granularity);

                case "subscription":
                    if (!dictionary.ContainsKey(str2))
                    {
                        throw new ArgumentException("Invalid value for value attribute: " + str2);
                    }
                    return new SubscriptionPrivacyRule(dictionary[str2], allow, order, granularity);
            }
            throw new ArgumentException("The value of the type attribute is invalid: " + attribute);
        }

        private Roster ParseRoster(XmlElement query)
        {
            Roster roster = new Roster(null);
            Dictionary<string, SubscriptionState> dictionary2 = new Dictionary<string, SubscriptionState>();
            dictionary2.Add("none", SubscriptionState.None);
            dictionary2.Add("to", SubscriptionState.To);
            dictionary2.Add("from", SubscriptionState.From);
            dictionary2.Add("both", SubscriptionState.Both);
            Dictionary<string, SubscriptionState> dictionary = dictionary2;
            XmlNodeList elementsByTagName = query.GetElementsByTagName("item");
            foreach (XmlElement element in elementsByTagName)
            {
                string attribute = element.GetAttribute("jid");
                if (!string.IsNullOrEmpty(attribute))
                {
                    string name = element.GetAttribute("name");
                    if (name == string.Empty)
                    {
                        name = null;
                    }
                    List<string> groups = new List<string>();
                    foreach (XmlElement element2 in element.GetElementsByTagName("group"))
                    {
                        groups.Add(element2.InnerText);
                    }
                    string key = element.GetAttribute("subscription");
                    SubscriptionState none = SubscriptionState.None;
                    if (dictionary.ContainsKey(key))
                    {
                        none = dictionary[key];
                    }
                    key = element.GetAttribute("ask");
                    roster.Add(new RosterItem(attribute, name, none, key == "subscribe", groups));
                }
            }
            return roster;
        }

        private void ProcessBeatIq(Iq iq)
        {
            Iq response = new Iq(IqType.Result, iq.Id, iq.From, this.Jid, null, null);
            this.core.IqResponse(response);
        }

        private void ProcessRosterIq(Iq iq)
        {
            Dictionary<string, SubscriptionState> dictionary2 = new Dictionary<string, SubscriptionState>();
            dictionary2.Add("none", SubscriptionState.None);
            dictionary2.Add("to", SubscriptionState.To);
            dictionary2.Add("from", SubscriptionState.From);
            dictionary2.Add("both", SubscriptionState.Both);
            Dictionary<string, SubscriptionState> dictionary = dictionary2;
            bool flag = ((iq.From == null) || (iq.From == this.Jid)) || (iq.From == this.Jid.GetBareJid());
            XmlNodeList elementsByTagName = iq.Data["query"].GetElementsByTagName("item");
            if (flag && (elementsByTagName.Count > 0))
            {
                XmlElement element = elementsByTagName.Item(0) as XmlElement;
                string attribute = element.GetAttribute("jid");
                if (!string.IsNullOrEmpty(attribute))
                {
                    string name = element.GetAttribute("name");
                    if (name == string.Empty)
                    {
                        name = null;
                    }
                    List<string> groups = new List<string>();
                    foreach (XmlElement element2 in element.GetElementsByTagName("group"))
                    {
                        groups.Add(element2.InnerText);
                    }
                    string key = element.GetAttribute("subscription");
                    SubscriptionState none = SubscriptionState.None;
                    if (dictionary.ContainsKey(key))
                    {
                        none = dictionary[key];
                    }
                    string str4 = element.GetAttribute("ask");
                    RosterItem item = new RosterItem(attribute, name, none, str4 == "subscribe", groups);
                    this.RosterUpdated.Raise<RosterUpdatedEventArgs>(this, new RosterUpdatedEventArgs(item, key == "remove"));
                }
                this.IqResult(iq, null);
            }
        }

        private void ProcessStatusNotification(S22.Xmpp.Im.Presence presence)
        {
            bool flag = presence.Type == PresenceType.Unavailable;
            XmlElement element = presence.Data["show"];
            Availability availability = flag ? Availability.Offline : Availability.Online;
            if (!flag && !((element == null) || string.IsNullOrEmpty(element.InnerText)))
            {
                string str = element.InnerText.Capitalize();
                availability = (Availability) Enum.Parse(typeof(Availability), str);
            }
            sbyte priority = 0;
            element = presence.Data["priority"];
            if (!((element == null) || string.IsNullOrEmpty(element.InnerText)))
            {
                priority = sbyte.Parse(element.InnerText);
            }
            string attribute = presence.Data.GetAttribute("xml:lang");
            Dictionary<string, string> messages = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(attribute))
            {
                attribute = this.core.Language.Name;
            }
            foreach (XmlNode node in presence.Data.GetElementsByTagName("status"))
            {
                XmlElement element2 = node as XmlElement;
                if (element2 != null)
                {
                    string str3 = element2.GetAttribute("xml:lang");
                    if (string.IsNullOrEmpty(str3))
                    {
                        str3 = attribute;
                    }
                    messages.Add(str3, element2.InnerText);
                }
            }
            S22.Xmpp.Im.Status status = new S22.Xmpp.Im.Status(availability, messages, priority);
            this.Status.Raise<StatusEventArgs>(this, new StatusEventArgs(presence.From, status));
        }

        private void ProcessSubscriptionRequest(S22.Xmpp.Im.Presence presence)
        {
            if ((this.SubscriptionRequest != null) && this.SubscriptionRequest(presence.From))
            {
                this.ApproveSubscriptionRequest(presence.From);
            }
            else
            {
                this.RefuseSubscriptionRequest(presence.From);
            }
        }

        private void ProcessSubscriptionResult(S22.Xmpp.Im.Presence presence)
        {
            if (presence.Type == PresenceType.Subscribed)
            {
                this.SubscriptionApproved.Raise<SubscriptionApprovedEventArgs>(this, new SubscriptionApprovedEventArgs(presence.From));
            }
            else
            {
                this.SubscriptionRefused.Raise<SubscriptionRefusedEventArgs>(this, new SubscriptionRefusedEventArgs(presence.From));
            }
        }

        private void ProcessUnsubscribeRequest(S22.Xmpp.Im.Presence presence)
        {
            this.Unsubscribed.Raise<UnsubscribedEventArgs>(this, new UnsubscribedEventArgs(presence.From));
        }

        public Roster ReConnect(string resource = null)
        {
            Roster roster2;
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            try
            {
                CommonConfig.Logger.WriteInfo("开始重连。。。");
                this.core.Connect(resource);
                this.IsHasRosterOnline = true;
                if (this.Username == null)
                {
                    return null;
                }
                this.EstablishSession();
                Roster roster = this.GetRoster();
                this.SendPresence(new S22.Xmpp.Im.Presence(null, null, PresenceType.Available, null, null, new XmlElement[0]));
                roster2 = roster;
            }
            catch (SocketException exception)
            {
                CommonConfig.Logger.WriteError(exception);
                throw new IOException("Could not connect to the server", exception);
            }
            return roster2;
        }

        public void RefuseSubscriptionRequest(S22.Xmpp.Jid jid)
        {
            this.AssertValid(true);
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            S22.Xmpp.Im.Presence presence = new S22.Xmpp.Im.Presence(jid, null, PresenceType.Unsubscribed, null, null, new XmlElement[0]);
            this.SendPresence(presence);
        }

        public void RemoveFromRoster(RosterItem item)
        {
            this.AssertValid(true);
            item.ThrowIfNull<RosterItem>("item");
            this.RemoveFromRoster(item.Jid);
        }

        public void RemoveFromRoster(S22.Xmpp.Jid jid)
        {
            this.AssertValid(true);
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            XmlElement data = Xml.Element("query", "jabber:iq:roster").Child(Xml.Element("item", null).Attr("jid", jid.ToString()).Attr("subscription", "remove"));
            Iq errorIq = this.IqRequest(IqType.Set, null, this.Jid, data, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The item could not be removed from the roster.");
            }
        }

        public void RemovePrivacyList(string name)
        {
            this.AssertValid(true);
            name.ThrowIfNull<string>("name");
            XmlElement data = Xml.Element("query", "jabber:iq:privacy").Child(Xml.Element("list", null).Attr("name", name));
            Iq errorIq = this.IqRequest(IqType.Set, null, this.Jid, data, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The privacy list could not be removed.");
            }
        }

        public void RequestSubscription(S22.Xmpp.Jid jid)
        {
            this.AssertValid(true);
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            S22.Xmpp.Im.Presence presence = new S22.Xmpp.Im.Presence(jid, null, PresenceType.Subscribe, null, null, new XmlElement[0]);
            this.SendPresence(presence);
        }

        public void RevokeSubscription(S22.Xmpp.Jid jid)
        {
            this.AssertValid(true);
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            S22.Xmpp.Im.Presence presence = new S22.Xmpp.Im.Presence(jid, null, PresenceType.Unsubscribed, null, null, new XmlElement[0]);
            this.SendPresence(presence);
        }

        public void SendMessage(S22.Xmpp.Im.Message message)
        {
            this.AssertValid(true);
            message.ThrowIfNull<S22.Xmpp.Im.Message>("message");
            message.From = this.Jid;
            foreach (XmppExtension extension in this.extensions)
            {
                IOutputFilter<S22.Xmpp.Im.Message> filter = extension as IOutputFilter<S22.Xmpp.Im.Message>;
                if (filter != null)
                {
                    filter.Output(message);
                }
            }
            this.core.SendMessage(message);
        }

        public void SendMessage(S22.Xmpp.Jid to, string body, string subject = null, string thread = null, MessageType type = 0, CultureInfo language = null)
        {
            this.AssertValid(true);
            to.ThrowIfNull<S22.Xmpp.Jid>("to");
            body.ThrowIfNullOrEmpty("body");
            S22.Xmpp.Im.Message message = new S22.Xmpp.Im.Message(to, body, subject, thread, type, language);
            this.SendMessage(message);
        }

        public void SendMessage(S22.Xmpp.Jid to, IDictionary<string, string> bodies, IDictionary<string, string> subjects = null, string thread = null, MessageType type = 0, CultureInfo language = null)
        {
            this.AssertValid(true);
            to.ThrowIfNull<S22.Xmpp.Jid>("to");
            bodies.ThrowIfNull<IDictionary<string, string>>("bodies");
            S22.Xmpp.Im.Message message = new S22.Xmpp.Im.Message(to, bodies, subjects, thread, type, language);
            this.SendMessage(message);
        }

        internal void SendPresence(S22.Xmpp.Im.Presence presence)
        {
            presence.ThrowIfNull<S22.Xmpp.Im.Presence>("presence");
            foreach (XmppExtension extension in this.extensions)
            {
                IOutputFilter<S22.Xmpp.Im.Presence> filter = extension as IOutputFilter<S22.Xmpp.Im.Presence>;
                if (filter != null)
                {
                    filter.Output(presence);
                }
            }
            this.core.SendPresence(presence);
        }

        public void SetActivePrivacyList(string name = null)
        {
            this.AssertValid(true);
            XmlElement data = Xml.Element("query", "jabber:iq:privacy").Child(Xml.Element("active", null));
            if (name != null)
            {
                data["active"].Attr("name", name);
            }
            Iq errorIq = this.IqRequest(IqType.Set, null, this.Jid, data, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The privacy list could not be activated.");
            }
        }

        public void SetDefaultPrivacyList(string name = null)
        {
            this.AssertValid(true);
            XmlElement data = Xml.Element("query", "jabber:iq:privacy").Child(Xml.Element("default", null));
            if (name != null)
            {
                data["default"].Attr("name", name);
            }
            Iq errorIq = this.IqRequest(IqType.Set, null, this.Jid, data, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The privacy list could not be made the default.");
            }
        }

        public void SetStatus(S22.Xmpp.Im.Status status)
        {
            this.AssertValid(true);
            status.ThrowIfNull<S22.Xmpp.Im.Status>("status");
            this.SetStatus(status.Availability, status.Messages, status.Priority);
        }

        public void SetStatus(Availability availability, Dictionary<string, string> messages, sbyte priority = 0)
        {
            this.AssertValid(true);
            if (availability == Availability.Offline)
            {
                throw new InvalidOperationException("Invalid availability state.");
            }
            List<XmlElement> list = new List<XmlElement>();
            if (availability != Availability.Online)
            {
                Dictionary<Availability, string> dictionary2 = new Dictionary<Availability, string>();
                dictionary2.Add(Availability.Away, "away");
                dictionary2.Add(Availability.DoNotDisturb, "dnd");
                dictionary2.Add(Availability.ExtendedAway, "xa");
                dictionary2.Add(Availability.Chat, "chat");
                Dictionary<Availability, string> dictionary = dictionary2;
                list.Add(Xml.Element("show", null).Text(dictionary[availability]));
            }
            if (priority != 0)
            {
                list.Add(Xml.Element("priority", null).Text(priority.ToString()));
            }
            if (messages != null)
            {
                foreach (KeyValuePair<string, string> pair in messages)
                {
                    XmlElement e = Xml.Element("status", null).Attr("xml:lang", pair.Key);
                    list.Add(e.Text(pair.Value));
                }
            }
            S22.Xmpp.Im.Presence presence = new S22.Xmpp.Im.Presence(null, null, PresenceType.Available, null, null, list.ToArray());
            this.SendPresence(presence);
        }

        public void SetStatus(Availability availability, string message = null, sbyte priority = 0, CultureInfo language = null)
        {
            this.AssertValid(true);
            if (availability == Availability.Offline)
            {
                throw new ArgumentException("Invalid availability state.");
            }
            List<XmlElement> list = new List<XmlElement>();
            if (availability != Availability.Online)
            {
                Dictionary<Availability, string> dictionary2 = new Dictionary<Availability, string>();
                dictionary2.Add(Availability.Away, "away");
                dictionary2.Add(Availability.DoNotDisturb, "dnd");
                dictionary2.Add(Availability.ExtendedAway, "xa");
                dictionary2.Add(Availability.Chat, "chat");
                Dictionary<Availability, string> dictionary = dictionary2;
                list.Add(Xml.Element("show", null).Text(dictionary[availability]));
            }
            if (priority != 0)
            {
                list.Add(Xml.Element("priority", null).Text(priority.ToString()));
            }
            if (message != null)
            {
                list.Add(Xml.Element("status", null).Text(message));
            }
            S22.Xmpp.Im.Presence presence = new S22.Xmpp.Im.Presence(null, null, PresenceType.Available, null, language, list.ToArray());
            this.SendPresence(presence);
        }

        private void SetupEventHandlers()
        {
            this.core.Iq += (sender, e) => this.OnIq(e.Stanza);
            this.core.Presence += (sender, e) => this.OnPresence(new S22.Xmpp.Im.Presence(e.Stanza));
            this.core.Message += (sender, e) => this.OnMessage(new S22.Xmpp.Im.Message(e.Stanza));
            this.core.Error += (sender, e) => this.Error.Raise<S22.Xmpp.Im.ErrorEventArgs>(sender, new S22.Xmpp.Im.ErrorEventArgs(e.Exception));
        }

        internal bool UnloadExtension<T>() where T: XmppExtension
        {
            XmppExtension item = this.GetExtension<T>();
            return ((item != null) ? this.extensions.Remove(item) : false);
        }

        public void Unsubscribe(S22.Xmpp.Jid jid)
        {
            this.AssertValid(true);
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            S22.Xmpp.Im.Presence presence = new S22.Xmpp.Im.Presence(jid, null, PresenceType.Unsubscribe, null, null, new XmlElement[0]);
            this.SendPresence(presence);
        }

        public bool Authenticated
        {
            get
            {
                return this.core.Authenticated;
            }
        }

        public bool Connected
        {
            get
            {
                return this.core.Connected;
            }
        }

        internal IEnumerable<XmppExtension> Extensions
        {
            get
            {
                return this.extensions;
            }
        }

        public string Hostname
        {
            get
            {
                return this.core.Hostname;
            }
            set
            {
                this.core.Hostname = value;
            }
        }

        public bool IsEncrypted
        {
            get
            {
                return this.core.IsEncrypted;
            }
        }

        public bool IsHasRosterOnline
        {
            get
            {
                if (DateTime.Now > this._lastCheckRosterTime.AddSeconds(302.0))
                {
                    return false;
                }
                return true;
            }
            set
            {
                this._lastCheckRosterTime = DateTime.Now;
            }
        }

        public S22.Xmpp.Jid Jid
        {
            get
            {
                return this.core.Jid;
            }
        }

        public string Password
        {
            get
            {
                return this.core.Password;
            }
            set
            {
                this.core.Password = value;
            }
        }

        public int Port
        {
            get
            {
                return this.core.Port;
            }
            set
            {
                this.core.Port = value;
            }
        }

        public S22.Xmpp.Im.SubscriptionRequest SubscriptionRequest { get; set; }

        public bool Tls
        {
            get
            {
                return this.core.Tls;
            }
            set
            {
                this.core.Tls = value;
            }
        }

        public string Username
        {
            get
            {
                return this.core.Username;
            }
            set
            {
                this.core.Username = value;
            }
        }

        public RemoteCertificateValidationCallback Validate
        {
            get
            {
                return this.core.Validate;
            }
            set
            {
                this.core.Validate = value;
            }
        }
    }
}

