namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using S22.Xmpp.Core.Sasl;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    public class XmppCore : IDisposable
    {
        private CancellationTokenSource cancelDispatch;
        private CancellationTokenSource cancelIq;
        private TcpClient client;
        private bool disposed;
        private string hostname;
        private int id;
        private ConcurrentDictionary<string, Action<string, S22.Xmpp.Core.Iq>> iqCallbacks;
        private ConcurrentDictionary<string, S22.Xmpp.Core.Iq> iqResponses;
        private StreamParser parser;
        private string password;
        private int port;
        private int randId;
        private string resource;
        private BlockingCollection<Stanza> stanzaQueue;
        private Stream stream;
        private string username;
        private ConcurrentDictionary<string, AutoResetEvent> waitHandles;
        private readonly object writeLock;

        public event EventHandler<S22.Xmpp.Core.ErrorEventArgs> Error;

        public event EventHandler<IqEventArgs> Iq;

        public event EventHandler<MessageEventArgs> Message;

        public event EventHandler<PresenceEventArgs> Presence;

        public XmppCore(string hostname, int port = 0x1466, bool tls = true, RemoteCertificateValidationCallback validate = null)
        {
            this.randId = new Random().Next(0xf4240, 0x98967f);
            this.writeLock = new object();
            this.waitHandles = new ConcurrentDictionary<string, AutoResetEvent>();
            this.iqResponses = new ConcurrentDictionary<string, S22.Xmpp.Core.Iq>();
            this.iqCallbacks = new ConcurrentDictionary<string, Action<string, S22.Xmpp.Core.Iq>>();
            this.cancelIq = new CancellationTokenSource();
            this.stanzaQueue = new BlockingCollection<Stanza>();
            this.cancelDispatch = new CancellationTokenSource();
            this.Hostname = hostname;
            this.ServerIP = hostname;
            this.Port = port;
            this.Tls = tls;
            this.Validate = validate;
        }

        public XmppCore(string hostname, string username, string password, int port = 0x1466, bool tls = true, RemoteCertificateValidationCallback validate = null)
        {
            this.randId = new Random().Next(0xf4240, 0x98967f);
            this.writeLock = new object();
            this.waitHandles = new ConcurrentDictionary<string, AutoResetEvent>();
            this.iqResponses = new ConcurrentDictionary<string, S22.Xmpp.Core.Iq>();
            this.iqCallbacks = new ConcurrentDictionary<string, Action<string, S22.Xmpp.Core.Iq>>();
            this.cancelIq = new CancellationTokenSource();
            this.stanzaQueue = new BlockingCollection<Stanza>();
            this.cancelDispatch = new CancellationTokenSource();
            this.Hostname = hostname;
            this.ServerIP = hostname;
            this.Username = username;
            this.Password = password;
            this.Port = port;
            this.Tls = tls;
            this.Validate = validate;
        }

        private void AssertValid()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
        }

        public void Authenticate(string username, string password)
        {
            this.AssertValid();
            username.ThrowIfNull<string>("username");
            password.ThrowIfNull<string>("password");
            if (this.Authenticated)
            {
                throw new XmppException("Authentication has already been performed.");
            }
            this.Username = username;
            this.Password = password;
            this.Disconnect();
            this.Connect(this.resource);
        }

        private XmlElement Authenticate(IEnumerable<string> mechanisms, string username, string password, string hostname)
        {
            string name = this.SelectMechanism(mechanisms);
            SaslMechanism mechanism = SaslFactory.Create(name);
            mechanism.Properties.Add("Username", username);
            mechanism.Properties.Add("Password", password);
            XmlElement element = Xml.Element("auth", "urn:ietf:params:xml:ns:xmpp-sasl").Attr("mechanism", name).Text(mechanism.HasInitial ? mechanism.GetResponse(string.Empty) : string.Empty);
            this.Send(element);
            while (true)
            {
                XmlElement element2 = this.parser.NextElement(new string[] { "challenge", "success", "failure" });
                if (element2.Name == "failure")
                {
                    throw new SaslException("SASL authentication failed.");
                }
                if ((element2.Name == "success") && mechanism.IsCompleted)
                {
                    break;
                }
                string response = mechanism.GetResponse(element2.InnerText);
                if (element2.Name == "success")
                {
                    if (!(response == string.Empty))
                    {
                        throw new SaslException("Could not verify server's signature.");
                    }
                    break;
                }
                element = Xml.Element("response", "urn:ietf:params:xml:ns:xmpp-sasl").Text(response);
                this.Send(element);
            }
            this.Authenticated = true;
            return this.InitiateStream(hostname);
        }

        private S22.Xmpp.Jid BindResource(string resourceName = null)
        {
            XmlElement e = Xml.Element("iq", null).Attr("type", "set").Attr("id", "bind-0");
            XmlElement element2 = Xml.Element("bind", "urn:ietf:params:xml:ns:xmpp-bind");
            if (resourceName != null)
            {
                element2.Child(Xml.Element("resource", null).Text(resourceName));
            }
            e.Child(element2);
            XmlElement element3 = this.SendAndReceive(e, new string[] { "iq" });
            if ((element3["bind"] == null) || (element3["bind"]["jid"] == null))
            {
                throw new XmppException("Erroneous server response.");
            }
            return new S22.Xmpp.Jid(element3["bind"]["jid"].InnerText);
        }

        public void Close()
        {
            this.AssertValid();
            this.Disconnect();
            this.Dispose();
        }

        public void Connect(string resource = null)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            this.resource = resource;
            try
            {
                this.client = new TcpClient(this.ServerIP, this.Port);
                this.stream = this.client.GetStream();
                this.SetupConnection(this.resource);
                this.Connected = true;
                Task.Factory.StartNew(new Action(this.ReadXmlStream), TaskCreationOptions.LongRunning);
                Task.Factory.StartNew(new Action(this.DispatchEvents), TaskCreationOptions.LongRunning);
            }
            catch (XmlException exception)
            {
                throw new XmppException("The XML stream could not be negotiated.", exception);
            }
        }

        private void Disconnect()
        {
            if (this.Connected)
            {
                this.Send("</stream:stream>");
                this.Connected = false;
                this.Authenticated = false;
            }
        }

        private void DispatchEvents()
        {
            while (true)
            {
                try
                {
                    Stanza stanza = this.stanzaQueue.Take(this.cancelDispatch.Token);
                    if (stanza is S22.Xmpp.Core.Iq)
                    {
                        CommonConfig.Logger.WriteInfo("IQ消息出列");
                        S22.Xmpp.Core.Iq iq = stanza as S22.Xmpp.Core.Iq;
                        this.Iq.Raise<IqEventArgs>(this, new IqEventArgs(stanza as S22.Xmpp.Core.Iq));
                    }
                    else if (stanza is S22.Xmpp.Core.Message)
                    {
                        this.Message.Raise<MessageEventArgs>(this, new MessageEventArgs(stanza as S22.Xmpp.Core.Message));
                    }
                    else if (stanza is S22.Xmpp.Core.Presence)
                    {
                        CommonConfig.Logger.WriteInfo("Presence消息出列");
                        this.Presence.Raise<PresenceEventArgs>(this, new PresenceEventArgs(stanza as S22.Xmpp.Core.Presence));
                    }
                }
                catch (OperationCanceledException exception)
                {
                    CommonConfig.Logger.WriteError("消息出列过程出错",exception);
                    //CommonConfig.Logger.WriteError(exception);
                    return;
                }
                catch (Exception exception2)
                {
                    CommonConfig.Logger.WriteError("消息出列过程出错", exception2);
                    //CommonConfig.Logger.WriteError(exception2);
                }
            }
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
                    if (this.parser != null)
                    {
                        this.parser.Close();
                    }
                    this.parser = null;
                    if (this.client != null)
                    {
                        this.client.Close();
                    }
                    this.client = null;
                }
            }
        }

        private string GetId()
        {
            Interlocked.Increment(ref this.id);
            return (this.randId + "-" + this.id.ToString());
        }

        private void HandleIqResponse(S22.Xmpp.Core.Iq iq)
        {
            AutoResetEvent event2;
            ThreadStart start = null;
            Action<string, S22.Xmpp.Core.Iq> cb;
            string id = iq.Id;
            this.iqResponses[id] = iq;
            if (this.waitHandles.TryRemove(id, out event2))
            {
                CommonConfig.Logger.WriteInfo(string.Concat(new object[] { id, "，队列数", this.waitHandles.Count, ",waitHandles清除队列成功，结果已返回，执行ev.Set()" }));
                event2.Set();
            }
            else if (this.iqCallbacks.TryRemove(id, out cb))
            {
                CommonConfig.Logger.WriteInfo(string.Concat(new object[] { id, "，队列数", this.iqCallbacks.Count, ",iqCallbacks清除队列成功，结果已返回，准备执行Task.Factory.StartNew(() => { cb(id, iq); });" }));
                if (start == null)
                {
                    start = delegate {
                        CommonConfig.Logger.WriteInfo("执行cb(id, iq);");
                        cb(id, iq);
                        CommonConfig.Logger.WriteInfo("执行完cb(id, iq);");
                    };
                }
                new Thread(start).Start();
            }
        }

        private XmlElement InitiateStream(string hostname)
        {
            XmlElement e = Xml.Element("stream:stream", "jabber:client").Attr("to", hostname).Attr("version", "1.0").Attr("xmlns:stream", "http://etherx.jabber.org/streams").Attr("xml:lang", CultureInfo.CurrentCulture.Name);
            this.Send(e.ToXmlString(true, true));
            if (this.parser != null)
            {
                this.parser.Close();
            }
            this.parser = new StreamParser(this.stream, true);
            this.Language = this.parser.Language ?? new CultureInfo("en");
            return this.parser.NextElement(new string[] { "stream:features" });
        }

        public S22.Xmpp.Core.Iq IqRequest(S22.Xmpp.Core.Iq request, int millisecondsTimeout = -1, string seqID = "")
        {
            S22.Xmpp.Core.Iq iq;
            this.AssertValid();
            request.ThrowIfNull<S22.Xmpp.Core.Iq>("request");
            if ((request.Type != IqType.Set) && (request.Type != IqType.Get))
            {
                throw new ArgumentException("The IQ type must be either 'set' or 'get'.");
            }
            request.Id = !string.IsNullOrEmpty(seqID) ? seqID : this.GetId();
            AutoResetEvent event2 = new AutoResetEvent(false);
            this.Send(request);
            this.waitHandles[request.Id] = event2;
            switch (WaitHandle.WaitAny(new WaitHandle[] { event2, this.cancelIq.Token.WaitHandle }, millisecondsTimeout))
            {
                case 0x102:
                    throw new TimeoutException();

                case 1:
                    CommonConfig.Logger.WriteInfo("The incoming XML stream could not read.");
                    throw new IOException("The incoming XML stream could not read.");
            }
            if (!this.iqResponses.TryRemove(request.Id, out iq))
            {
                throw new InvalidOperationException();
            }
            return iq;
        }

        public S22.Xmpp.Core.Iq IqRequest(IqType type, S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, XmlElement data = null, CultureInfo language = null, int millisecondsTimeout = -1)
        {
            this.AssertValid();
            return this.IqRequest(new S22.Xmpp.Core.Iq(type, null, to, from, data, language), millisecondsTimeout, "");
        }

        public string IqRequestAsync(S22.Xmpp.Core.Iq request, Action<string, S22.Xmpp.Core.Iq> callback = null)
        {
            this.AssertValid();
            request.ThrowIfNull<S22.Xmpp.Core.Iq>("request");
            if ((request.Type != IqType.Set) && (request.Type != IqType.Get))
            {
                throw new ArgumentException("The IQ type must be either 'set' or 'get'.");
            }
            request.Id = this.GetId();
            if (callback != null)
            {
                this.iqCallbacks[request.Id] = callback;
            }
            this.Send(request);
            return request.Id;
        }

        public string IqRequestAsync(IqType type, S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, XmlElement data = null, CultureInfo language = null, Action<string, S22.Xmpp.Core.Iq> callback = null)
        {
            this.AssertValid();
            return this.IqRequestAsync(new S22.Xmpp.Core.Iq(type, null, to, from, data, language), callback);
        }

        public void IqResponse(S22.Xmpp.Core.Iq response)
        {
            this.AssertValid();
            response.ThrowIfNull<S22.Xmpp.Core.Iq>("response");
            if ((response.Type != IqType.Result) && (response.Type != IqType.Error))
            {
                throw new ArgumentException("The IQ type must be either 'result' or 'error'.");
            }
            this.Send(response);
        }

        public void IqResponse(IqType type, string id, S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, XmlElement data = null, CultureInfo language = null)
        {
            this.AssertValid();
            this.IqResponse(new S22.Xmpp.Core.Iq(type, id, to, from, data, null));
        }

        private void ReadXmlStream()
        {
            try
            {
                Stopwatch stopwatch;
                bool flag;
                goto Label_01A0;
            Label_0007:
                stopwatch = new Stopwatch();
                stopwatch.Start();
                //CommonConfig.Logger.WriteInfo("读取流--开始。。。");
                XmlElement element = this.parser.NextElement(new string[] { "iq", "message", "presence" });
                //CommonConfig.Logger.WriteInfo("读取流--解析Xml,耗时：" + stopwatch.ElapsedMilliseconds);
                if (element["data"] == null)
                {
                    CommonConfig.Logger.WriteInfo("接收到数据：" + element.OuterXml);
                }
                else
                {
                    XmlNode node = element.Clone();
                    node["data"].InnerText = "";
                    CommonConfig.Logger.WriteInfo("接收到文件：" + node.OuterXml);
                }
                string name = element.Name;
                if (name != null)
                {
                    if (!(name == "iq"))
                    {
                        if (name == "message")
                        {
                            goto Label_0150;
                        }
                        if (name == "presence")
                        {
                            goto Label_0164;
                        }
                    }
                    else
                    {
                        S22.Xmpp.Core.Iq item = new S22.Xmpp.Core.Iq(element);
                        if (item.IsRequest)
                        {
                            this.stanzaQueue.Add(item);
                        }
                        else
                        {
                            this.HandleIqResponse(item);
                        }
                    }
                }
                goto Label_0178;
            Label_0150:
                this.stanzaQueue.Add(new S22.Xmpp.Core.Message(element));
                goto Label_0178;
            Label_0164:
                this.stanzaQueue.Add(new S22.Xmpp.Core.Presence(element));
            Label_0178:
                CommonConfig.Logger.WriteInfo(string.Format("读取流--结束，耗时:【{0}】", stopwatch.ElapsedMilliseconds));
                stopwatch.Stop();
            Label_01A0:
                flag = true;
                goto Label_0007;
            }
            catch (Exception exception)
            {
                CommonConfig.Logger.WriteError("接收异常，此处认为是已经断开了。", exception);
                this.cancelDispatch.Cancel();
                this.cancelDispatch = new CancellationTokenSource();
                this.cancelIq.Cancel();
                this.cancelIq = new CancellationTokenSource();
                this.Connected = false;
                if (!this.disposed)
                {
                    this.Error.Raise<S22.Xmpp.Core.ErrorEventArgs>(this, new S22.Xmpp.Core.ErrorEventArgs(exception));
                }
            }
        }

        public void ReConnect(string resource = null)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            this.resource = resource;
            try
            {
                this.client = new TcpClient(this.ServerIP, this.Port);
                this.stream = this.client.GetStream();
                this.SetupConnection(this.resource);
                this.Connected = true;
            }
            catch (XmlException exception)
            {
                throw new XmppException("The XML stream could not be negotiated.", exception);
            }
        }

        private string SelectMechanism(IEnumerable<string> mechanisms)
        {
            string[] strArray = new string[] { "PLAIN", "SCRAM-SHA-1", "DIGEST-MD5" };
            for (int i = 0; i < strArray.Length; i++)
            {
                if (mechanisms.Contains<string>(strArray[i], StringComparer.InvariantCultureIgnoreCase))
                {
                    return strArray[i];
                }
            }
            throw new SaslException("No supported SASL mechanism found.");
        }

        private void Send(Stanza stanza)
        {
            stanza.ThrowIfNull<Stanza>("stanza");
            this.Send(stanza.ToString());
        }

        private void Send(string xml)
        {
            xml.ThrowIfNull<string>("xml");
            if (xml.Contains("data xmlns="))
            {
                int length = 0xd3;
                string str = "";
                if (xml.Length < 0xd3)
                {
                    length = xml.Length;
                }
                if (xml.Length > 0)
                {
                    str = xml.Substring(0, length);
                }
                CommonConfig.Logger.WriteInfo("发送文件数据(前211个字符)：" + str);
            }
            else
            {
                CommonConfig.Logger.WriteInfo("发送数据：" + xml.Replace("&quot;", "'"));
            }
            byte[] bytes = Encoding.UTF8.GetBytes(xml);
            lock (this.writeLock)
            {
                this.stream.Write(bytes, 0, bytes.Length);
            }
        }

        private void Send(XmlElement element)
        {
            element.ThrowIfNull<XmlElement>("element");
            this.Send(element.ToXmlString(false, false));
        }

        private XmlElement SendAndReceive(XmlElement element, params string[] expected)
        {
            this.Send(element);
            return this.parser.NextElement(expected);
        }

        public void SendMessage(S22.Xmpp.Core.Message message)
        {
            this.AssertValid();
            message.ThrowIfNull<S22.Xmpp.Core.Message>("message");
            this.Send(message);
        }

        public void SendMessage(S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, XmlElement data = null, string id = null, CultureInfo language = null)
        {
            this.AssertValid();
            this.Send(new S22.Xmpp.Core.Message(to, from, data, id, language));
        }

        public void SendPresence(S22.Xmpp.Core.Presence presence)
        {
            this.AssertValid();
            presence.ThrowIfNull<S22.Xmpp.Core.Presence>("presence");
            this.Send(presence);
        }

        public void SendPresence(S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, string id = null, CultureInfo language = null, params XmlElement[] data)
        {
            this.AssertValid();
            this.Send(new S22.Xmpp.Core.Presence(to, from, id, language, data));
        }

        public void SetHostNameByJID(string jid)
        {
            string str = jid.Split(new char[] { '@' })[1];
            string str2 = str.Split(new char[] { '/' })[0];
            this.Hostname = str2;
        }

        private void SetupConnection(string resource = null)
        {
            XmlElement element = this.InitiateStream(this.Hostname);
            if (element["starttls"] != null)
            {
                if (!((element["starttls"]["required"] == null) || this.Tls))
                {
                    throw new AuthenticationException("The server requires TLS/SSL.");
                }
                if (this.Tls)
                {
                    element = this.StartTls(this.Hostname, this.Validate);
                }
            }
            if (this.Username != null)
            {
                XmlElement element2 = element["mechanisms"];
                if (!((element2 != null) && element2.HasChildNodes))
                {
                    throw new AuthenticationException("No SASL mechanisms advertised.");
                }
                XmlNode firstChild = element2.FirstChild;
                HashSet<string> mechanisms = new HashSet<string>();
                while (firstChild != null)
                {
                    mechanisms.Add(firstChild.InnerText);
                    firstChild = firstChild.NextSibling;
                }
                try
                {
                    if (this.Authenticate(mechanisms, this.Username, this.Password, this.Hostname)["bind"] != null)
                    {
                        this.Jid = this.BindResource(resource);
                        this.Hostname = this.Jid.Domain;
                    }
                }
                catch (SaslException exception)
                {
                    throw new AuthenticationException("Authentication failed.", exception);
                }
            }
        }

        private XmlElement StartTls(string hostname, RemoteCertificateValidationCallback validate)
        {
            this.SendAndReceive(Xml.Element("starttls", "urn:ietf:params:xml:ns:xmpp-tls"), new string[] { "proceed" });
            SslStream stream = new SslStream(this.stream, false, validate ?? ((sender, cert, chain, err) => true));
            stream.AuthenticateAsClient(hostname);
            this.stream = stream;
            this.IsEncrypted = true;
            return this.InitiateStream(hostname);
        }

        public bool Authenticated { get; private set; }

        public bool Connected { get; private set; }

        public string Hostname
        {
            get
            {
                return this.hostname;
            }
            set
            {
                value.ThrowIfNullOrEmpty("Hostname");
                this.hostname = value;
            }
        }

        public bool IsEncrypted { get; private set; }

        public S22.Xmpp.Jid Jid { get; private set; }

        public CultureInfo Language { get; private set; }

        public string Password
        {
            get
            {
                return this.password;
            }
            set
            {
                value.ThrowIfNull<string>("Password");
                this.password = value;
            }
        }

        public int Port
        {
            get
            {
                return this.port;
            }
            set
            {
                value.ThrowIfOutOfRange("Port", 0, 0x10000);
                this.port = value;
            }
        }

        public string ServerIP { get; set; }

        public bool Tls { get; set; }

        public string Username
        {
            get
            {
                return this.username;
            }
            set
            {
                value.ThrowIfNullOrEmpty("Username");
                this.username = value;
            }
        }

        public RemoteCertificateValidationCallback Validate { get; set; }
    }
}

