namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Extensions.Dataforms;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Xml;

    internal class SIFileTransfer : XmppExtension
    {
        private EntityCapabilities ecapa;
        private ConcurrentDictionary<string, FileMetaData> metaData;
        private const string mimeType = "application/octet-stream";
        private ConcurrentDictionary<string, SISession> siSessions;
        private StreamInitiation streamInitiation;
        private static readonly Type[] supportedMethods = new Type[] { typeof(Socks5Bytestreams), typeof(InBandBytestreams) };

        public event EventHandler<FileTransferAbortedEventArgs> FileTransferAborted;

        public event EventHandler<FileTransferProgressEventArgs> FileTransferProgress;

        public SIFileTransfer(XmppIm im) : base(im)
        {
            this.siSessions = new ConcurrentDictionary<string, SISession>();
            this.metaData = new ConcurrentDictionary<string, FileMetaData>();
        }

        public void CancelFileTransfer(FileTransfer transfer)
        {
            transfer.ThrowIfNull<FileTransfer>("transfer");
            SISession session = this.GetSession(transfer.SessionId, transfer.From, transfer.To);
            if (session == null)
            {
                throw new ArgumentException("The specified transfer instance does not represent an active data-transfer operation.");
            }
            session.Extension.CancelTransfer(session);
        }

        public SISession GetSession(string sid, Jid from, Jid to)
        {
            SISession session;
            sid.ThrowIfNull<string>("sid");
            from.ThrowIfNull<Jid>("from");
            to.ThrowIfNull<Jid>("to");
            if (!this.siSessions.TryGetValue(sid, out session))
            {
                return null;
            }
            if ((session.From != from) || (session.To != to))
            {
                return null;
            }
            return session;
        }

        private IEnumerable<string> GetStreamMethods()
        {
            ISet<string> set = new HashSet<string>();
            foreach (Type type in supportedMethods)
            {
                if (((!this.ForceInBandBytestreams || !(type != typeof(InBandBytestreams))) && ((CommonConfig.FileTranType != 0) || !(type != typeof(InBandBytestreams)))) && ((CommonConfig.FileTranType != 1) || !(type != typeof(Socks5Bytestreams))))
                {
                    XmppExtension extension = base.im.GetExtension(type);
                    if (extension != null)
                    {
                        foreach (string str in extension.Namespaces)
                        {
                            set.Add(str);
                        }
                    }
                }
            }
            return set;
        }

        public string GetUriFileExtend(string filePath)
        {
            filePath = filePath.Replace(@"\", "/").Replace("//", "/").Replace("//", "/");
            string[] strArray = filePath.Split(new char[] { '/' });
            if (strArray.Length > 0)
            {
                return strArray[strArray.Length - 1].ToLower();
            }
            return "";
        }

        public override void Initialize()
        {
            this.streamInitiation = base.im.GetExtension<StreamInitiation>();
            this.streamInitiation.RegisterProfile("http://jabber.org/protocol/si/profile/file-transfer", new Func<Jid, XmlElement, XmlElement>(this.OnStreamInitiationRequest));
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
            foreach (Type type in supportedMethods)
            {
                XmppExtension extension = base.im.GetExtension(type);
                if (!((extension != null) && (extension is IDataStream)))
                {
                    throw new XmppException("Invalid data-stream type: " + type);
                }
                IDataStream stream = extension as IDataStream;
                stream.BytesTransferred += new EventHandler<BytesTransferredEventArgs>(this.OnBytesTransferred);
                stream.TransferAborted += new EventHandler<TransferAbortedEventArgs>(this.OnTransferAborted);
            }
        }

        public void InitiateFileTransfer(Jid to, string path, string description = null, Action<bool, FileTransfer> cb = null)
        {
            to.ThrowIfNull<Jid>("to");
            path.ThrowIfNull<string>("path");
            CommonConfig.Logger.WriteInfo("进入InitiateFileTransfer方法发送文件：" + path);
            Stream stream = null;
            string name = "";
            long size = 0L;
            try
            {
                FileInfo info;
                name = this.GetUriFileExtend(path);
                if (path.ToLower().Contains("http"))
                {
                    string str2 = CommonConfig.TempFilePath + "/" + DateTime.Now.ToString("yyyyMMdd");
                    if (!Directory.Exists(str2))
                    {
                        Directory.CreateDirectory(str2);
                    }
                    string filePath = str2 + "/" + DateTime.Now.ToString("HHmmss_") + name;
                    if (!HttpHelper.HttpDown(path, filePath, ""))
                    {
                        throw new Exception("文件下载失败！");
                    }
                    info = new FileInfo(filePath);
                    size = info.Length;
                    stream = File.OpenRead(filePath);
                    CommonConfig.Logger.WriteInfo(filePath + "下载完成");
                }
                else
                {
                    info = new FileInfo(path);
                    name = info.Name;
                    size = info.Length;
                    stream = File.OpenRead(path);
                }
            }
            catch (Exception exception)
            {
                CommonConfig.Logger.WriteError("读取待发送文件过程出错！", exception);
                throw exception;
            }
            this.InitiateFileTransfer(to, stream, name, size, description, cb);
        }

        public void InitiateFileTransfer(Jid to, Stream stream, string name, long size, string description = null, Action<bool, FileTransfer> cb = null)
        {
            to.ThrowIfNull<Jid>("to");
            stream.ThrowIfNull<Stream>("stream");
            name.ThrowIfNull<string>("name");
            size.ThrowIfOutOfRange(0L, 0x7fffffffffffffffL);
            if (!this.ecapa.Supports(to, new Extension[] { Extension.SIFileTransfer }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'SI File Transfer' extension.");
            }
            this.InitiateStreamAsync(to, name, size, description, delegate (InitiationResult result, Iq iq) {
                CommonConfig.Logger.WriteInfo("发送文件回调完成：进入OnInitiationResult");
                if (result == null)
                {
                    CommonConfig.Logger.WriteInfo("发送文件过程出错,IQ节为：" + iq.ToString());
                }
                this.OnInitiationResult(result, to, name, stream, size, description, cb);
            });
        }

        private void InitiateStreamAsync(Jid to, string name, long size, string description = null, Action<InitiationResult, Iq> cb = null)
        {
            XmlElement e = Xml.Element("file", "http://jabber.org/protocol/si/profile/file-transfer").Attr("name", name).Attr("size", size.ToString());
            if (description != null)
            {
                e.Child(Xml.Element("desc", null).Text(description));
            }
            IEnumerable<string> streamMethods = this.GetStreamMethods();
            this.streamInitiation.InitiateStreamAsync(to, "application/octet-stream", "http://jabber.org/protocol/si/profile/file-transfer", streamMethods, e, cb);
        }

        public void InvalidateSession(string sid)
        {
            SISession session;
            sid.ThrowIfNull<string>("sid");
            if (this.siSessions.TryRemove(sid, out session) && (session.Stream != null))
            {
                session.Stream.Dispose();
            }
        }

        private void OnBytesTransferred(object sender, BytesTransferredEventArgs e)
        {
            FileMetaData data;
            if (this.metaData.TryGetValue(e.Session.Sid, out data))
            {
                this.FileTransferProgress.Raise<FileTransferProgressEventArgs>(this, new FileTransferProgressEventArgs(new FileTransfer(e.Session, data.Name, data.Description)));
            }
        }

        private void OnInitiationResult(InitiationResult result, Jid to, string name, Stream stream, long size, string description, Action<bool, FileTransfer> cb)
        {
            Exception exception;
            FileTransfer transfer = new FileTransfer(base.im.Jid, to, name, size, null, description, 0L);
            try
            {
                IDataStream extension = base.im.GetExtension(result.Method) as IDataStream;
                SISession session = new SISession(result.SessionId, stream, size, false, base.im.Jid, to, extension);
                this.siSessions.TryAdd(result.SessionId, session);
                this.metaData.TryAdd(result.SessionId, new FileMetaData(name, description));
                if (cb != null)
                {
                    cb(true, transfer);
                }
                try
                {
                    CommonConfig.Logger.WriteInfo("开始执行Transfer文件传输方法");
                    extension.Transfer(session);
                }
                catch (Exception exception1)
                {
                    exception = exception1;
                    CommonConfig.Logger.WriteError("发送文件过程出错：" + exception.ToString(), exception);
                }
            }
            catch (Exception exception2)
            {
                exception = exception2;
                if (stream != null)
                {
                    stream.Dispose();
                }
                CommonConfig.Logger.WriteError("OnInitiationResult发送文件异常", exception);
                if (cb != null)
                {
                    cb(false, transfer);
                }
            }
        }

        private XmlElement OnStreamInitiationRequest(Jid from, XmlElement si)
        {
            FileStream stream = null;
            try
            {
                string str = this.SelectStreamMethod(si["feature"]);
                string attribute = si.GetAttribute("id");
                if (string.IsNullOrEmpty(attribute) || this.siSessions.ContainsKey(attribute))
                {
                    return new XmppError(ErrorType.Cancel, ErrorCondition.Conflict, new XmlElement[0]).Data;
                }
                XmlElement element = si["file"];
                string description = (element["desc"] != null) ? element["desc"].InnerText : null;
                string name = element.GetAttribute("name");
                int num = int.Parse(element.GetAttribute("size"));
                FileTransfer transfer = new FileTransfer(from, base.im.Jid, name, (long) num, attribute, description, 0L);
                string path = this.TransferRequest(transfer);
                if (path == null)
                {
                    return new XmppError(ErrorType.Cancel, ErrorCondition.NotAcceptable, new XmlElement[0]).Data;
                }
                stream = File.OpenWrite(path);
                SISession session = new SISession(attribute, stream, (long) num, true, from, base.im.Jid, base.im.GetExtension(str) as IDataStream);
                this.siSessions.TryAdd(attribute, session);
                this.metaData.TryAdd(attribute, new FileMetaData(name, description));
                return Xml.Element("si", "http://jabber.org/protocol/si").Child(FeatureNegotiation.Create(new SubmitForm(new DataField[] { new ListField("stream-method", str) })));
            }
            catch
            {
                if (stream != null)
                {
                    stream.Close();
                }
                return new XmppError(ErrorType.Cancel, ErrorCondition.BadRequest, new XmlElement[0]).Data;
            }
        }

        private void OnTransferAborted(object sender, TransferAbortedEventArgs e)
        {
            FileMetaData data;
            if (this.metaData.TryGetValue(e.Session.Sid, out data))
            {
                this.FileTransferAborted.Raise<FileTransferAbortedEventArgs>(this, new FileTransferAbortedEventArgs(new FileTransfer(e.Session, data.Name, data.Description)));
            }
        }

        private string SelectStreamMethod(XmlElement feature)
        {
            ListField field = FeatureNegotiation.Parse(feature).Fields["stream-method"] as ListField;
            string[] strArray = new string[] { "http://jabber.org/protocol/bytestreams", "http://jabber.org/protocol/ibb" };
            for (int i = 0; i < strArray.Length; i++)
            {
                if ((!this.ForceInBandBytestreams || !(strArray[i] != "http://jabber.org/protocol/ibb")) && field.Values.Contains<string>(strArray[i]))
                {
                    return strArray[i];
                }
            }
            throw new ArgumentException("No supported method advertised.");
        }

        private bool SupportsNamespace(string @namespace)
        {
            @namespace.ThrowIfNull<string>("namespace");
            foreach (XmppExtension extension in base.im.Extensions)
            {
                foreach (string str in extension.Namespaces)
                {
                    if (str == @namespace)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ForceInBandBytestreams { get; set; }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/si/profile/file-transfer" };
            }
        }

        public FileTransferRequest TransferRequest { get; set; }

        public override Extension Xep
        {
            get
            {
                return Extension.SIFileTransfer;
            }
        }
    }
}

