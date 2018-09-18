namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Core;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Xml;

    internal class InBandBytestreams : XmppExtension, IInputFilter<Iq>, IDataStream
    {
        private const int blockSize = 0x1000;
        private EntityCapabilities ecapa;
        private SIFileTransfer siFileTransfer;

        public event EventHandler<BytesTransferredEventArgs> BytesTransferred;

        public event EventHandler<TransferAbortedEventArgs> TransferAborted;

        public InBandBytestreams(XmppIm im) : base(im)
        {
        }

        public void CancelTransfer(SISession session)
        {
            session.ThrowIfNull<SISession>("session");
            this.siFileTransfer.InvalidateSession(session.Sid);
            this.TransferAborted.Raise<TransferAbortedEventArgs>(this, new TransferAbortedEventArgs(session));
        }

        private void Close(string sessionId, Iq stanza)
        {
            sessionId.ThrowIfNull<string>("sessionId");
            stanza.ThrowIfNull<Iq>("stanza");
            SISession session = this.siFileTransfer.GetSession(sessionId, stanza.From, stanza.To);
            if (session != null)
            {
                this.siFileTransfer.InvalidateSession(sessionId);
                if (session.Count < session.Size)
                {
                    this.TransferAborted.Raise<TransferAbortedEventArgs>(this, new TransferAbortedEventArgs(session));
                }
            }
        }

        private void CloseStream(Jid to, string sessionId)
        {
            this.siFileTransfer.InvalidateSession(sessionId);
            if (!this.ecapa.Supports(to, new Extension[] { Extension.InBandBytestreams }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'In-Band Bytestreams' extension.");
            }
            XmlElement data = Xml.Element("close", "http://jabber.org/protocol/ibb").Attr("sid", sessionId);
            base.im.IqRequestAsync(IqType.Set, to, base.im.Jid, data, null, null);
        }

        private void Data(string sessionId, Iq stanza)
        {
            sessionId.ThrowIfNull<string>("sessionId");
            stanza.ThrowIfNull<Iq>("stanza");
            XmlElement element = stanza.Data["data"];
            if (element == null)
            {
                throw new ArgumentException("Invalid stanza, missing data element.");
            }
            SISession session = this.siFileTransfer.GetSession(sessionId, stanza.From, base.im.Jid);
            if (session == null)
            {
                throw new ArgumentException("Invalid session-id.");
            }
            byte[] buffer = Convert.FromBase64String(element.InnerText);
            try
            {
                session.Stream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception exception)
            {
                throw new IOException("The stream could not be written.", exception);
            }
            session.Count += buffer.Length;
            this.BytesTransferred.Raise<BytesTransferredEventArgs>(this, new BytesTransferredEventArgs(session));
        }

        public override void Initialize()
        {
            this.siFileTransfer = base.im.GetExtension<SIFileTransfer>();
            this.ecapa = base.im.GetExtension<EntityCapabilities>();
        }

        public bool Input(Iq stanza)
        {
            if (stanza.Type != IqType.Set)
            {
                return false;
            }
            XmlElement element = stanza.Data["open"];
            if (element == null)
            {
                element = stanza.Data["close"];
            }
            if (element == null)
            {
                element = stanza.Data["data"];
            }
            if ((element == null) || (element.NamespaceURI != "http://jabber.org/protocol/ibb"))
            {
                return false;
            }
            string attribute = element.GetAttribute("sid");
            try
            {
                string name = element.Name;
                if (name == null)
                {
                    goto Label_00F1;
                }
                if (!(name == "open"))
                {
                    if (name == "data")
                    {
                        goto Label_00DB;
                    }
                    if (name == "close")
                    {
                        goto Label_00E6;
                    }
                    goto Label_00F1;
                }
                this.Open(attribute, stanza);
                goto Label_00FC;
            Label_00DB:
                this.Data(attribute, stanza);
                goto Label_00FC;
            Label_00E6:
                this.Close(attribute, stanza);
                goto Label_00FC;
            Label_00F1:
                throw new ArgumentException("Invalid stanza element.");
            Label_00FC:
                base.im.IqResult(stanza, null);
            }
            catch (Exception exception)
            {
                base.im.IqError(stanza, ErrorType.Cancel, ErrorCondition.ServiceUnavailable, exception.Message, new XmlElement[0]);
                this.siFileTransfer.InvalidateSession(attribute);
            }
            return true;
        }

        private void Open(string sessionId, Iq stanza)
        {
            sessionId.ThrowIfNull<string>("sessionId");
            stanza.ThrowIfNull<Iq>("stanza");
            if (this.siFileTransfer.GetSession(sessionId, stanza.From, base.im.Jid) == null)
            {
                throw new XmppException("Invalid session-id.");
            }
            string attribute = stanza.Data["open"].GetAttribute("stanza");
            if (!(string.IsNullOrEmpty(attribute) || !(attribute != "iq")))
            {
                throw new XmppException("Only IQ stanzas are supported.");
            }
        }

        private void OpenStream(Jid to, string sessionId)
        {
            if (!this.ecapa.Supports(to, new Extension[] { Extension.InBandBytestreams }))
            {
                throw new NotSupportedException("The XMPP entity does not support the 'In-Band Bytestreams' extension.");
            }
            int num = 0x1000;
            XmlElement data = Xml.Element("open", "http://jabber.org/protocol/ibb").Attr("block-size", num.ToString()).Attr("sid", sessionId).Attr("stanza", "iq");
            Iq errorIq = base.im.IqRequest(IqType.Set, to, base.im.Jid, data, null, -1, "");
            if (errorIq.Type == IqType.Error)
            {
                throw Util.ExceptionFromError(errorIq, "The in-band bytestream could not be opened.");
            }
        }

        public void Transfer(SISession session)
        {
            session.ThrowIfNull<SISession>("session");
            this.OpenStream(session.To, session.Sid);
            byte[] buffer = new byte[0x1000];
            ushort num = 0;
            long size = session.Size;
            try
            {
                while (size > 0L)
                {
                    int length = session.Stream.Read(buffer, 0, 0x1000);
                    size -= length;
                    if (length <= 0)
                    {
                        return;
                    }
                    string text = Convert.ToBase64String(buffer, 0, length);
                    XmlElement data = Xml.Element("data", "http://jabber.org/protocol/ibb").Attr("sid", session.Sid).Attr("seq", num.ToString()).Text(text);
                    num = (ushort) (num + 1);
                    Iq errorIq = base.im.IqRequest(IqType.Set, session.To, base.im.Jid, data, null, -1, "");
                    if (errorIq.Type == IqType.Error)
                    {
                        throw Util.ExceptionFromError(errorIq, null);
                    }
                    session.Count += length;
                    this.BytesTransferred.Raise<BytesTransferredEventArgs>(this, new BytesTransferredEventArgs(session));
                }
            }
            catch (ObjectDisposedException exception)
            {
                CommonConfig.Logger.WriteError(exception);
            }
            catch (Exception exception2)
            {
                CommonConfig.Logger.WriteError(exception2);
                this.TransferAborted.Raise<TransferAbortedEventArgs>(this, new TransferAbortedEventArgs(session));
                throw;
            }
            finally
            {
                this.CloseStream(session.To, session.Sid);
            }
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/ibb" };
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.InBandBytestreams;
            }
        }
    }
}

