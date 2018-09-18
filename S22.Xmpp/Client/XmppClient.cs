namespace S22.Xmpp.Client
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Extensions;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net.Security;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public class XmppClient : IDisposable
    {
        private Attention attention;
        private BitsOfBinary bitsOfBinary;
        private BlockingCommand block;
        private ChatStateNotifications chatStateNotifications;
        private DataForms dataForms;
        private bool disposed;
        private EntityCapabilities ecapa;
        private FeatureNegotiation featureNegotiation;
        private XmppIm im;
        private InBandBytestreams inBandBytestreams;
        private InBandRegistration inBandRegistration;
        private Pep pep;
        private S22.Xmpp.Extensions.Ping ping;
        private ServiceDiscovery sdisco;
        private ServerIpCheck serverIpCheck;
        private SIFileTransfer siFileTransfer;
        private Socks5Bytestreams socks5Bytestreams;
        private StreamInitiation streamInitiation;
        private EntityTime time;
        private UserActivity userActivity;
        private UserAvatar userAvatar;
        private UserMood userMood;
        private UserTune userTune;
        private SoftwareVersion version;

        public event EventHandler<ActivityChangedEventArgs> ActivityChanged
        {
            add
            {
                this.userActivity.ActivityChanged += value;
            }
            remove
            {
                this.userActivity.ActivityChanged -= value;
            }
        }

        public event EventHandler<AvatarChangedEventArgs> AvatarChanged
        {
            add
            {
                this.userAvatar.AvatarChanged += value;
            }
            remove
            {
                this.userAvatar.AvatarChanged -= value;
            }
        }

        public event EventHandler<ChatStateChangedEventArgs> ChatStateChanged
        {
            add
            {
                this.chatStateNotifications.ChatStateChanged += value;
            }
            remove
            {
                this.chatStateNotifications.ChatStateChanged -= value;
            }
        }

        public event EventHandler<S22.Xmpp.Im.ErrorEventArgs> Error
        {
            add
            {
                this.im.Error += value;
            }
            remove
            {
                this.im.Error -= value;
            }
        }

        public event EventHandler<FileTransferAbortedEventArgs> FileTransferAborted
        {
            add
            {
                this.siFileTransfer.FileTransferAborted += value;
            }
            remove
            {
                this.siFileTransfer.FileTransferAborted -= value;
            }
        }

        public event EventHandler<FileTransferProgressEventArgs> FileTransferProgress
        {
            add
            {
                this.siFileTransfer.FileTransferProgress += value;
            }
            remove
            {
                this.siFileTransfer.FileTransferProgress -= value;
            }
        }

        public event EventHandler<S22.Xmpp.Im.IqEventArgs> IqRequestEvents
        {
            add
            {
                this.im.IqRequestEvents += value;
            }
            remove
            {
                this.im.IqRequestEvents -= value;
            }
        }

        public event EventHandler<S22.Xmpp.Im.IqEventArgs> IqResponseEvents
        {
            add
            {
                this.im.IqResponseEvents += value;
            }
            remove
            {
                this.im.IqResponseEvents -= value;
            }
        }

        public event EventHandler<S22.Xmpp.Im.MessageEventArgs> Message
        {
            add
            {
                this.im.Message += value;
            }
            remove
            {
                this.im.Message -= value;
            }
        }

        public event EventHandler<MoodChangedEventArgs> MoodChanged
        {
            add
            {
                this.userMood.MoodChanged += value;
            }
            remove
            {
                this.userMood.MoodChanged -= value;
            }
        }

        public event EventHandler<RosterUpdatedEventArgs> RosterUpdated
        {
            add
            {
                this.im.RosterUpdated += value;
            }
            remove
            {
                this.im.RosterUpdated -= value;
            }
        }

        public event EventHandler<StatusEventArgs> StatusChanged
        {
            add
            {
                this.im.Status += value;
            }
            remove
            {
                this.im.Status -= value;
            }
        }

        public event EventHandler<SubscriptionApprovedEventArgs> SubscriptionApproved
        {
            add
            {
                this.im.SubscriptionApproved += value;
            }
            remove
            {
                this.im.SubscriptionApproved -= value;
            }
        }

        public event EventHandler<SubscriptionRefusedEventArgs> SubscriptionRefused
        {
            add
            {
                this.im.SubscriptionRefused += value;
            }
            remove
            {
                this.im.SubscriptionRefused -= value;
            }
        }

        public event EventHandler<TuneEventArgs> Tune
        {
            add
            {
                this.userTune.Tune += value;
            }
            remove
            {
                this.userTune.Tune -= value;
            }
        }

        public event EventHandler<UnsubscribedEventArgs> Unsubscribed
        {
            add
            {
                this.im.Unsubscribed += value;
            }
            remove
            {
                this.im.Unsubscribed -= value;
            }
        }

        public XmppClient(string hostname, int port = 0x1466, bool tls = true, RemoteCertificateValidationCallback validate = null)
        {
            this.im = new XmppIm(hostname, port, tls, validate);
            this.LoadExtensions();
        }

        public XmppClient(string hostname, string username, string password, int port = 0x1466, bool tls = true, RemoteCertificateValidationCallback validate = null)
        {
            this.im = new XmppIm(hostname, username, password, port, tls, validate);
            this.LoadExtensions();
        }

        public void AddContact(S22.Xmpp.Jid jid, string name = null, params string[] groups)
        {
            this.AssertValid();
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.im.AddToRoster(new RosterItem(jid, name, groups));
            this.im.RequestSubscription(jid);
        }

        private void AssertValid()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            if (!this.Connected)
            {
                throw new InvalidOperationException("Not connected to XMPP server.");
            }
            if (!this.Authenticated)
            {
                throw new InvalidOperationException("Not authenticated with XMPP server.");
            }
        }

        public void Authenticate(string username, string password)
        {
            this.im.Autenticate(username, password);
        }

        public void Block(S22.Xmpp.Jid jid)
        {
            this.AssertValid();
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            if (this.block.Supported)
            {
                this.block.Block(jid);
            }
            else
            {
                PrivacyList privacyList = null;
                string defaultPrivacyList = this.im.GetDefaultPrivacyList();
                if (defaultPrivacyList != null)
                {
                    privacyList = this.im.GetPrivacyList(defaultPrivacyList);
                }
                foreach (PrivacyList list2 in this.im.GetPrivacyLists())
                {
                    if (list2.Name == "blocklist")
                    {
                        privacyList = list2;
                    }
                }
                if (privacyList == null)
                {
                    privacyList = new PrivacyList("blocklist");
                }
                privacyList.Add(new JidPrivacyRule(jid, false, 0, 0), true);
                this.im.EditPrivacyList(privacyList);
                this.im.SetDefaultPrivacyList(privacyList.Name);
                this.im.SetActivePrivacyList(privacyList.Name);
            }
        }

        public void Buzz(S22.Xmpp.Jid jid, string message = null)
        {
            this.AssertValid();
            this.attention.GetAttention(jid, message);
        }

        public void CancelFileTransfer(FileTransfer transfer)
        {
            this.AssertValid();
            transfer.ThrowIfNull<FileTransfer>("transfer");
            this.siFileTransfer.CancelFileTransfer(transfer);
        }

        public void Close()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
            this.Dispose();
        }

        public void Connect(string resource = null)
        {
            this.im.Connect(resource);
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
                    if (this.im != null)
                    {
                        this.im.Close();
                    }
                    this.im = null;
                }
            }
        }

        public IEnumerable<S22.Xmpp.Jid> GetBlocklist()
        {
            this.AssertValid();
            if (this.block.Supported)
            {
                return this.block.GetBlocklist();
            }
            PrivacyList privacyList = null;
            string defaultPrivacyList = this.im.GetDefaultPrivacyList();
            if (defaultPrivacyList != null)
            {
                privacyList = this.im.GetPrivacyList(defaultPrivacyList);
            }
            foreach (PrivacyList list2 in this.im.GetPrivacyLists())
            {
                if (list2.Name == "blocklist")
                {
                    privacyList = list2;
                }
            }
            HashSet<S22.Xmpp.Jid> set = new HashSet<S22.Xmpp.Jid>();
            if (privacyList != null)
            {
                foreach (PrivacyRule rule in privacyList)
                {
                    if (rule is JidPrivacyRule)
                    {
                        set.Add((rule as JidPrivacyRule).Jid);
                    }
                }
            }
            return set;
        }

        public IEnumerable<Extension> GetFeatures(S22.Xmpp.Jid jid)
        {
            this.AssertValid();
            return this.ecapa.GetExtensions(jid);
        }

        public Roster GetRoster()
        {
            this.AssertValid();
            return this.im.GetRoster();
        }

        public DateTime GetTime(S22.Xmpp.Jid jid)
        {
            this.AssertValid();
            return this.time.GetTime(jid);
        }

        public VersionInformation GetVersion(S22.Xmpp.Jid jid)
        {
            this.AssertValid();
            return this.version.GetVersion(jid);
        }

        public void InitiateFileTransfer(S22.Xmpp.Jid to, string path, string description = null, Action<bool, FileTransfer> cb = null)
        {
            this.AssertValid();
            this.siFileTransfer.InitiateFileTransfer(to, path, description, cb);
        }

        public void InitiateFileTransfer(S22.Xmpp.Jid to, Stream stream, string name, long size, string description = null, Action<bool, FileTransfer> cb = null)
        {
            this.AssertValid();
            this.siFileTransfer.InitiateFileTransfer(to, stream, name, size, description, cb);
        }

        public void IqRequestBeatJieShun(S22.Xmpp.Jid to)
        {
            if (this.im != null)
            {
                this.im.IqRequestBeatJieShun(to, this.Jid, IqType.Get);
            }
        }

        public Iq IqRequestJieShun(S22.Xmpp.Jid to, string cnt, int crc, string mode, IqType iqType = 0, int millisecondsTimeout = -1, string seqID = "")
        {
            if (millisecondsTimeout == -1)
            {
                millisecondsTimeout = 0x4e20;
            }
            return this.im.IqRequestJieShun(to, this.Jid, cnt, crc, mode, iqType, millisecondsTimeout, seqID);
        }

        public void IqResponseJieShun(IqType type, string id, string cnt, int crc, int rc, string sync, S22.Xmpp.Jid to = null, S22.Xmpp.Jid from = null, CultureInfo language = null)
        {
            this.im.IqResponseJieShun(type, id, cnt, crc, rc, sync, to, from, language);
        }

        private void LoadExtensions()
        {
            this.version = this.im.LoadExtension<SoftwareVersion>();
            this.sdisco = this.im.LoadExtension<ServiceDiscovery>();
            this.ecapa = this.im.LoadExtension<EntityCapabilities>();
            this.ping = this.im.LoadExtension<S22.Xmpp.Extensions.Ping>();
            this.attention = this.im.LoadExtension<Attention>();
            this.time = this.im.LoadExtension<EntityTime>();
            this.block = this.im.LoadExtension<BlockingCommand>();
            this.pep = this.im.LoadExtension<Pep>();
            this.userTune = this.im.LoadExtension<UserTune>();
            this.userAvatar = this.im.LoadExtension<UserAvatar>();
            this.userMood = this.im.LoadExtension<UserMood>();
            this.dataForms = this.im.LoadExtension<DataForms>();
            this.featureNegotiation = this.im.LoadExtension<FeatureNegotiation>();
            this.streamInitiation = this.im.LoadExtension<StreamInitiation>();
            this.siFileTransfer = this.im.LoadExtension<SIFileTransfer>();
            this.inBandBytestreams = this.im.LoadExtension<InBandBytestreams>();
            this.userActivity = this.im.LoadExtension<UserActivity>();
            this.socks5Bytestreams = this.im.LoadExtension<Socks5Bytestreams>();
            this.FileTransferSettings = new S22.Xmpp.Client.FileTransferSettings(this.socks5Bytestreams, this.siFileTransfer);
            this.serverIpCheck = this.im.LoadExtension<ServerIpCheck>();
            this.inBandRegistration = this.im.LoadExtension<InBandRegistration>();
            this.chatStateNotifications = this.im.LoadExtension<ChatStateNotifications>();
            this.bitsOfBinary = this.im.LoadExtension<BitsOfBinary>();
        }

        public TimeSpan Ping(S22.Xmpp.Jid jid)
        {
            this.AssertValid();
            return this.ping.PingEntity(jid);
        }

        public void ReConnect(string resource = null)
        {
            this.im.ReConnect(resource);
        }

        public void Register(RegistrationCallback callback)
        {
            callback.ThrowIfNull<RegistrationCallback>("callback");
            this.inBandRegistration.Register(callback);
        }

        public void RemoveContact(RosterItem item)
        {
            this.AssertValid();
            item.ThrowIfNull<RosterItem>("item");
            this.im.RemoveFromRoster(item);
        }

        public void RemoveContact(S22.Xmpp.Jid jid)
        {
            this.AssertValid();
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.im.RemoveFromRoster(jid);
        }

        public void SendMessage(S22.Xmpp.Im.Message message)
        {
            this.AssertValid();
            message.ThrowIfNull<S22.Xmpp.Im.Message>("message");
            this.im.SendMessage(message);
        }

        public void SendMessage(S22.Xmpp.Jid to, IDictionary<string, string> bodies, IDictionary<string, string> subjects = null, string thread = null, MessageType type = 0, CultureInfo language = null)
        {
            this.AssertValid();
            to.ThrowIfNull<S22.Xmpp.Jid>("to");
            bodies.ThrowIfNull<IDictionary<string, string>>("bodies");
            this.im.SendMessage(to, bodies, subjects, thread, type, language);
        }

        public void SendMessage(S22.Xmpp.Jid to, string body, string subject = null, string thread = null, MessageType type = 0, CultureInfo language = null)
        {
            this.AssertValid();
            to.ThrowIfNull<S22.Xmpp.Jid>("to");
            body.ThrowIfNullOrEmpty("body");
            this.im.SendMessage(to, body, subject, thread, type, language);
        }

        public void SendPresence()
        {
            if (this.im != null)
            {
                this.im.SendPresence(new S22.Xmpp.Im.Presence());
            }
        }

        public void SetActivity(GeneralActivity activity, SpecificActivity specific =  SpecificActivity.Other, string description = null)
        {
            this.AssertValid();
            this.userActivity.SetActivity(activity, specific, description);
        }

        public void SetAvatar(string filePath)
        {
            this.AssertValid();
            filePath.ThrowIfNull<string>("filePath");
            this.userAvatar.Publish(filePath);
        }

        public void SetMood(Mood mood, string description = null)
        {
            this.AssertValid();
            this.userMood.SetMood(mood, description);
        }

        public void SetStatus(Status status)
        {
            this.AssertValid();
            status.ThrowIfNull<Status>("status");
            this.im.SetStatus(status);
        }

        public void SetStatus(Availability availability, Dictionary<string, string> messages, sbyte priority = 0)
        {
            this.AssertValid();
            this.im.SetStatus(availability, messages, priority);
        }

        public void SetStatus(Availability availability, string message = null, sbyte priority = 0, CultureInfo language = null)
        {
            this.AssertValid();
            this.im.SetStatus(availability, message, 0, language);
        }

        public void SetTune(TuneInformation tune)
        {
            this.AssertValid();
            this.userTune.Publish(tune);
        }

        public void SetTune(string title = null, string artist = null, string track = null, int length = 0, int rating = 0, string source = null, string uri = null)
        {
            this.AssertValid();
            this.userTune.Publish(title, artist, track, length, rating, source, uri);
        }

        public void Unblock(S22.Xmpp.Jid jid)
        {
            this.AssertValid();
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            if (this.block.Supported)
            {
                this.block.Unblock(jid);
            }
            else
            {
                PrivacyList privacyList = null;
                string defaultPrivacyList = this.im.GetDefaultPrivacyList();
                if (defaultPrivacyList != null)
                {
                    privacyList = this.im.GetPrivacyList(defaultPrivacyList);
                }
                foreach (PrivacyList list2 in this.im.GetPrivacyLists())
                {
                    if (list2.Name == "blocklist")
                    {
                        privacyList = list2;
                    }
                }
                if (privacyList != null)
                {
                    ISet<JidPrivacyRule> set = new HashSet<JidPrivacyRule>();
                    foreach (PrivacyRule rule in privacyList)
                    {
                        if (rule is JidPrivacyRule)
                        {
                            JidPrivacyRule item = rule as JidPrivacyRule;
                            if (!(!(item.Jid == jid) || item.Allow))
                            {
                                set.Add(item);
                            }
                        }
                    }
                    foreach (JidPrivacyRule rule3 in set)
                    {
                        privacyList.Remove(rule3);
                    }
                    if (privacyList.Count == 0)
                    {
                        this.im.SetDefaultPrivacyList(null);
                        this.im.RemovePrivacyList(privacyList.Name);
                    }
                    else
                    {
                        this.im.EditPrivacyList(privacyList);
                        this.im.SetDefaultPrivacyList(privacyList.Name);
                    }
                }
            }
        }

        public bool Authenticated
        {
            get
            {
                return this.im.Authenticated;
            }
        }

        public bool Connected
        {
            get
            {
                if (this.im == null)
                {
                    return false;
                }
                return this.im.Connected;
            }
        }

        public S22.Xmpp.Extensions.FileTransferRequest FileTransferRequest
        {
            get
            {
                return this.siFileTransfer.TransferRequest;
            }
            set
            {
                this.siFileTransfer.TransferRequest = value;
            }
        }

        public S22.Xmpp.Client.FileTransferSettings FileTransferSettings { get; private set; }

        public string Hostname
        {
            get
            {
                return this.im.Hostname;
            }
            set
            {
                this.im.Hostname = value;
            }
        }

        public XmppIm Im
        {
            get
            {
                return this.im;
            }
        }

        public bool IsEncrypted
        {
            get
            {
                return this.im.IsEncrypted;
            }
        }

        public bool IsHasRosterOnline
        {
            get
            {
                return this.im.IsHasRosterOnline;
            }
        }

        public S22.Xmpp.Jid Jid
        {
            get
            {
                return this.im.Jid;
            }
        }

        public string Password
        {
            get
            {
                return this.im.Password;
            }
            set
            {
                this.im.Password = value;
            }
        }

        public int Port
        {
            get
            {
                return this.im.Port;
            }
            set
            {
                this.im.Port = value;
            }
        }

        public S22.Xmpp.Im.SubscriptionRequest SubscriptionRequest
        {
            get
            {
                return this.im.SubscriptionRequest;
            }
            set
            {
                this.im.SubscriptionRequest = value;
            }
        }

        public bool Tls
        {
            get
            {
                return this.im.Tls;
            }
            set
            {
                this.im.Tls = value;
            }
        }

        public string Username
        {
            get
            {
                return this.im.Username;
            }
            set
            {
                this.im.Username = value;
            }
        }

        public RemoteCertificateValidationCallback Validate
        {
            get
            {
                return this.im.Validate;
            }
            set
            {
                this.im.Validate = value;
            }
        }
    }
}

