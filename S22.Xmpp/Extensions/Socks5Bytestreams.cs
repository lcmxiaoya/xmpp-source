namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Extensions.Socks5;
    using S22.Xmpp.Extensions.Stun;
    using S22.Xmpp.Extensions.Upnp;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    internal class Socks5Bytestreams : XmppExtension, IInputFilter<Iq>, IDataStream
    {
        private const int acceptTimeout = 0x2bf20;
        private const int defaultPort = 0x438;
        private EntityCapabilities ecapa;
        private ServiceDiscovery sdisco;
        private ServerIpCheck serverIpCheck;
        private int serverPortFrom;
        private int serverPortTo;
        private SIFileTransfer siFileTransfer;

        public event EventHandler<BytesTransferredEventArgs> BytesTransferred;

        public event EventHandler<TransferAbortedEventArgs> TransferAborted;

        public Socks5Bytestreams(XmppIm im) : base(im)
        {
            this.serverPortFrom = 0xcb20;
            this.serverPortTo = 0xcb84;
            this.StunServer = new DnsEndPoint("stun.l.google.com", 0x4b66);
            this.ProxyAllowed = true;
            this.Proxies = new HashSet<Streamhost>();
            this.UseUPnP = true;
        }

        private void AcceptClientConnection(SISession session, Socks5Server server, int timeout = -1)
        {
            SocksRequest request = server.Accept(timeout);
            if (request.Command != SocksCommand.Connect)
            {
                throw new Socks5Exception("Unexpected SOCKS5 command: " + request.Command);
            }
            if (request.ATyp != ATyp.Domain)
            {
                throw new Socks5Exception("Unexpected ATyp: " + request.ATyp);
            }
            string destination = (string) request.Destination;
            if (this.Sha1(session.Sid + base.im.Jid + session.To) != destination)
            {
                throw new Socks5Exception("Hostname hash mismatch.");
            }
            server.Reply(ReplyStatus.Succeeded, destination, request.Port);
        }

        private bool BehindNAT(IPAddress address)
        {
            address.ThrowIfNull<IPAddress>("address");
            return (GetIpAddresses(null).SingleOrDefault<IPAddress>(addr => addr.Equals(address)) == null);
        }

        public void CancelTransfer(SISession session)
        {
            session.ThrowIfNull<SISession>("session");
            this.siFileTransfer.InvalidateSession(session.Sid);
        }

        private Socks5Server CreateSocks5Server(int portFrom, int portTo)
        {
            for (int i = portFrom; i <= portTo; i++)
            {
                try
                {
                    return new Socks5Server(i, null);
                }
                catch (SocketException exception)
                {
                    if (exception.SocketErrorCode != SocketError.AddressAlreadyInUse)
                    {
                        throw;
                    }
                }
            }
            throw new ArgumentException("All ports of the specified range are already in use.");
        }

        private void DirectTransfer(SISession session)
        {
            Func<IPAddress, bool> predicate = null;
            Socks5Server socks5Server = null;
            try
            {
                socks5Server = this.CreateSocks5Server(this.serverPortFrom, this.serverPortTo);
            }
            catch (Exception exception)
            {
                throw new Socks5Exception("The SOCKS5 server could not be created.", exception);
            }
            IEnumerable<IPAddress> source = null;
            try
            {
                source = this.GetExternalAddresses();
                if (predicate == null)
                {
                    predicate = addr => this.BehindNAT(addr);
                }
                if (source.Any<IPAddress>(predicate) && this.UseUPnP)
                {
                    try
                    {
                        UPnP.ForwardPort(socks5Server.Port, ProtocolType.Tcp, "XMPP SOCKS5 File-transfer");
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }
            catch (NotSupportedException)
            {
            }
            Task.Factory.StartNew(delegate {
                try
                {
                    this.AcceptClientConnection(session, socks5Server, 0x2bf20);
                    this.SendData(session, socks5Server.GetStream());
                }
                finally
                {
                    socks5Server.Close();
                }
            });
            XmlElement e = Xml.Element("query", "http://jabber.org/protocol/bytestreams").Attr("sid", session.Sid);
            ISet<IPAddress> set = new HashSet<IPAddress>();
            if (source != null)
            {
                set.UnionWith(source);
            }
            set.UnionWith(GetIpAddresses(null));
            foreach (IPAddress address in set)
            {
                e.Child(Xml.Element("streamhost", null).Attr("jid", base.im.Jid.ToString()).Attr("host", address.ToString()).Attr("port", socks5Server.Port.ToString()));
            }
            Iq errorIq = base.im.IqRequest(IqType.Set, session.To, base.im.Jid, e, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The SOCKS5 connection could not be established.");
            }
        }

        private Socks5Client EstablishConnection(Iq stanza, string sid, IEnumerable<Streamhost> hosts)
        {
            bool flag = false;
            foreach (Streamhost streamhost in hosts)
            {
                try
                {
                    Socks5Client client = new Socks5Client(streamhost.Host, streamhost.Port, null, null);
                    flag = true;
                    string domain = this.Sha1(sid + stanza.From + stanza.To);
                    if (client.Request(SocksCommand.Connect, domain, 0).Status != ReplyStatus.Succeeded)
                    {
                        throw new Socks5Exception("SOCKS5 Connect request failed.");
                    }
                    base.im.IqResult(stanza, Xml.Element("query", "http://jabber.org/protocol/bytestreams").Attr("sid", sid).Child(Xml.Element("streamhost-used", null).Attr("jid", streamhost.Jid.ToString())));
                    return client;
                }
                catch
                {
                    if (flag)
                    {
                        break;
                    }
                }
            }
            base.im.IqError(stanza, ErrorType.Cancel, ErrorCondition.ItemNotFound, null, new XmlElement[0]);
            throw new XmppException("Couldn't connect to streamhost.");
        }

        private IEnumerable<IPAddress> GetExternalAddresses()
        {
            ISet<IPAddress> set = new HashSet<IPAddress>();
            try
            {
                set.Add(this.serverIpCheck.GetExternalAddress());
            }
            catch
            {
            }
            if (this.UseUPnP)
            {
                try
                {
                    foreach (IPAddress address in UPnP.GetExternalAddresses())
                    {
                        set.Add(address);
                    }
                }
                catch (Exception)
                {
                }
            }
            try
            {
                set.Add(StunClient.Query(this.StunServer.Host, this.StunServer.Port, 0xbb8));
            }
            catch
            {
            }
            if (set.Count == 0)
            {
                throw new NotSupportedException("The external IP address(es) could not be obtained.");
            }
            return set;
        }

        public static IEnumerable<IPAddress> GetIpAddresses(IPAddress address = null)
        {
            ISet<IPAddress> set = new HashSet<IPAddress>();
            foreach (NetworkInterface interface2 in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (interface2.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation information in interface2.GetIPProperties().UnicastAddresses)
                    {
                        if ((!IPAddress.IsLoopback(information.Address) && (information.Address.AddressFamily == AddressFamily.InterNetwork)) && ((address == null) || information.Address.InSameSubnet(address, information.IPv4Mask)))
                        {
                            set.Add(information.Address);
                        }
                    }
                }
            }
            return set;
        }

        private Streamhost GetNetworkAddress(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            Iq errorIq = base.im.IqRequest(IqType.Get, jid, base.im.Jid, Xml.Element("query", "http://jabber.org/protocol/bytestreams"), null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The network address could not be retrieved.");
            }
            XmlElement query = errorIq.Data["query"];
            if ((query == null) || (query.NamespaceURI != "http://jabber.org/protocol/bytestreams"))
            {
                throw new XmppException("Erroneous server response.");
            }
            IEnumerable<Streamhost> source = this.ParseStreamhosts(query);
            if (source.Count<Streamhost>() < 1)
            {
                throw new XmppException("No streamhost element found.");
            }
            return source.First<Streamhost>();
        }

        private IEnumerable<Streamhost> GetProxyList()
        {
            ISet<Streamhost> set = new HashSet<Streamhost>();
            foreach (S22.Xmpp.Extensions.Item item in this.sdisco.GetItems(base.im.Jid.Domain))
            {
                foreach (Identity identity in this.sdisco.GetIdentities(item.Jid))
                {
                    if ((identity.Category == "proxy") && (identity.Type == "bytestreams"))
                    {
                        Streamhost networkAddress = this.GetNetworkAddress(item.Jid);
                        set.Add(networkAddress);
                    }
                }
            }
            return set;
        }

        public override void Initialize()
        {
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
            this.siFileTransfer = base.im.GetExtension<SIFileTransfer>();
            this.sdisco = base.im.GetExtension<ServiceDiscovery>();
            this.serverIpCheck = base.im.GetExtension<ServerIpCheck>();
        }

        public bool Input(Iq stanza)
        {
            if (stanza.Type != IqType.Set)
            {
                return false;
            }
            XmlElement query = stanza.Data["query"];
            if ((query == null) || (query.NamespaceURI != "http://jabber.org/protocol/bytestreams"))
            {
                return false;
            }
            string sid = query.GetAttribute("sid");
            if (!this.VerifySession(stanza, sid))
            {
                base.im.IqError(stanza, ErrorType.Modify, ErrorCondition.NotAcceptable, null, new XmlElement[0]);
                return true;
            }
            if (query.GetAttribute("mode") == "udp")
            {
                base.im.IqError(stanza, ErrorType.Modify, ErrorCondition.FeatureNotImplemented, "UDP-mode is not supported.", new XmlElement[0]);
                return true;
            }
            IEnumerable<Streamhost> hosts = this.ParseStreamhosts(query);
            if (hosts.Count<Streamhost>() == 0)
            {
                base.im.IqError(stanza, ErrorType.Modify, ErrorCondition.BadRequest, "No streamhosts advertised.", new XmlElement[0]);
                return true;
            }
            Task.Factory.StartNew(delegate {
                using (Socks5Client client = this.EstablishConnection(stanza, sid, hosts))
                {
                    this.ReceiveData(stanza, sid, client.GetStream());
                }
            });
            return true;
        }

        private void MediatedTransfer(SISession session, IEnumerable<Streamhost> proxies)
        {
            Streamhost streamhost = this.NegotiateProxy(session, proxies);
            using (Socks5Client client = new Socks5Client(streamhost.Host, streamhost.Port, null, null))
            {
                string domain = this.Sha1(session.Sid + session.From + session.To);
                if (client.Request(SocksCommand.Connect, domain, 0).Status != ReplyStatus.Succeeded)
                {
                    CommonConfig.Logger.WriteInfo("SOCKS5 Connect request failed.");
                    throw new Socks5Exception("SOCKS5 Connect request failed.");
                }
                CommonConfig.Logger.WriteInfo("SOCKS5 Connect successed.");
                XmlElement data = Xml.Element("query", "http://jabber.org/protocol/bytestreams").Attr("sid", session.Sid).Child(Xml.Element("activate", null).Text(session.To.ToString()));
                Iq errorIq = base.im.IqRequest(IqType.Set, streamhost.Jid, base.im.Jid, data, null, -1, "");
                if (errorIq.Type == IqType.Error)
                {
                    throw Util.ExceptionFromError(errorIq, "Could not activate the bytestream.");
                }
                this.SendData(session, client.GetStream());
            }
        }

        private Streamhost NegotiateProxy(SISession session, IEnumerable<Streamhost> proxies)
        {
            XmlElement e = Xml.Element("query", "http://jabber.org/protocol/bytestreams").Attr("sid", session.Sid);
            foreach (Streamhost streamhost in proxies)
            {
                e.Child(Xml.Element("streamhost", null).Attr("jid", streamhost.Jid.ToString()).Attr("host", streamhost.Host).Attr("port", streamhost.Port.ToString()));
            }
            Iq errorIq = base.im.IqRequest(IqType.Set, session.To, base.im.Jid, e, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The SOCKS5 negotiation failed.");
            }
            XmlElement element2 = errorIq.Data["query"];
            if ((element2 == null) || (element2.NamespaceURI != "http://jabber.org/protocol/bytestreams"))
            {
                throw new XmppException("Erroneous response.");
            }
            XmlElement element3 = element2["streamhost-used"];
            if (element3 == null)
            {
                throw new XmppException("Missing streamhost-used element.");
            }
            string proxyJid = element3.GetAttribute("jid");
            Streamhost streamhost2 = proxies.FirstOrDefault<Streamhost>(proxy => proxy.Jid == proxyJid);
            if (streamhost2 == null)
            {
                throw new XmppException("Invalid streamhost JID.");
            }
            return streamhost2;
        }

        private IEnumerable<Streamhost> ParseStreamhosts(XmlElement query)
        {
            IList<Streamhost> list = new List<Streamhost>();
            foreach (XmlElement element in query.GetElementsByTagName("streamhost"))
            {
                try
                {
                    string attribute = element.GetAttribute("jid");
                    string host = element.GetAttribute("host");
                    string str3 = element.GetAttribute("port");
                    int port = string.IsNullOrEmpty(str3) ? 0x438 : int.Parse(str3);
                    list.Add(new Streamhost(attribute, host, port));
                }
                catch
                {
                }
            }
            return list;
        }

        private void ReceiveData(Iq stanza, string sid, Stream stream)
        {
            SISession session = this.siFileTransfer.GetSession(sid, stanza.From, stanza.To);
            if (session == null)
            {
                throw new XmppException("Invalid session-id: " + sid);
            }
            long size = session.Size;
            try
            {
                while (size > 0L)
                {
                    byte[] buffer = new byte[0x1000];
                    int count = stream.Read(buffer, 0, buffer.Length);
                    if (count <= 0)
                    {
                        return;
                    }
                    size -= count;
                    session.Stream.Write(buffer, 0, count);
                    session.Count += count;
                    this.BytesTransferred.Raise<BytesTransferredEventArgs>(this, new BytesTransferredEventArgs(session));
                }
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                this.siFileTransfer.InvalidateSession(sid);
                if (session.Count < session.Size)
                {
                    this.TransferAborted.Raise<TransferAbortedEventArgs>(this, new TransferAbortedEventArgs(session));
                }
            }
        }

        private void SendData(SISession session, Stream stream)
        {
            long size = session.Size;
            try
            {
                CommonConfig.Logger.WriteInfo("开始通过socket5方式传输流，大小为：" + size);
                while (size > 0L)
                {
                    byte[] buffer = new byte[0x1000];
                    int count = session.Stream.Read(buffer, 0, (int) Math.Min(size, (long) buffer.Length));
                    if (count > 0)
                    {
                        stream.Write(buffer, 0, count);
                        CommonConfig.Logger.WriteInfo("socket5传read=" + count.ToString());
                    }
                    else
                    {
                        break;
                    }
                    size -= count;
                    session.Count += count;
                    this.BytesTransferred.Raise<BytesTransferredEventArgs>(this, new BytesTransferredEventArgs(session));
                }
                CommonConfig.Logger.WriteInfo("传输完成，socket5方式共：" + session.Size);
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                this.siFileTransfer.InvalidateSession(session.Sid);
                if (session.Count < session.Size)
                {
                    this.TransferAborted.Raise<TransferAbortedEventArgs>(this, new TransferAbortedEventArgs(session));
                }
            }
        }

        private string Sha1(string s)
        {
            s.ThrowIfNull<string>("s");
            using (SHA1Managed managed = new SHA1Managed())
            {
                byte[] buffer = managed.ComputeHash(Encoding.UTF8.GetBytes(s));
                StringBuilder builder = new StringBuilder();
                foreach (byte num in buffer)
                {
                    builder.Append(num.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public void Transfer(SISession session)
        {
            IEnumerable<Streamhost> source = null;
            if (this.ProxyAllowed)
            {
                try
                {
                    source = this.GetProxyList();
                }
                catch
                {
                }
            }
            try
            {
                if ((source != null) && (source.Count<Streamhost>() > 0))
                {
                    this.MediatedTransfer(session, source);
                }
                else
                {
                    this.DirectTransfer(session);
                }
            }
            catch (Exception exception)
            {
                CommonConfig.Logger.WriteError("socket5传输过程出现异常！", exception);
                this.TransferAborted.Raise<TransferAbortedEventArgs>(this, new TransferAbortedEventArgs(session));
                this.siFileTransfer.InvalidateSession(session.Sid);
            }
        }

        private bool VerifySession(Iq stanza, string sid)
        {
            if (string.IsNullOrEmpty(sid))
            {
                return false;
            }
            return (this.siFileTransfer.GetSession(sid, stanza.From, base.im.Jid) != null);
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/bytestreams" };
            }
        }

        public ICollection<Streamhost> Proxies { get; private set; }

        public bool ProxyAllowed { get; set; }

        public int ServerPortFrom
        {
            get
            {
                return this.serverPortFrom;
            }
            set
            {
                value.ThrowIfOutOfRange(0, this.ServerPortTo);
                this.serverPortFrom = value;
            }
        }

        public int ServerPortTo
        {
            get
            {
                return this.serverPortTo;
            }
            set
            {
                value.ThrowIfOutOfRange(this.ServerPortFrom, 0xffff);
                this.serverPortTo = value;
            }
        }

        public DnsEndPoint StunServer { get; set; }

        public bool UseUPnP { get; set; }

        public override Extension Xep
        {
            get
            {
                return Extension.Socks5Bytestreams;
            }
        }
    }
}

