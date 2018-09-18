namespace S22.Xmpp.Client
{
    using S22.Xmpp;
    using S22.Xmpp.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Net;

    public class FileTransferSettings
    {
        private SIFileTransfer siFileTransfer;
        private Socks5Bytestreams socks5;

        internal FileTransferSettings(Socks5Bytestreams socks5, SIFileTransfer siFileTransfer)
        {
            socks5.ThrowIfNull<Socks5Bytestreams>("socks5");
            siFileTransfer.ThrowIfNull<SIFileTransfer>("siFileTransfer");
            this.socks5 = socks5;
            this.siFileTransfer = siFileTransfer;
        }

        public bool ForceInBandBytestreams
        {
            get
            {
                return this.siFileTransfer.ForceInBandBytestreams;
            }
            set
            {
                this.siFileTransfer.ForceInBandBytestreams = value;
            }
        }

        public ICollection<Streamhost> Proxies
        {
            get
            {
                return this.socks5.Proxies;
            }
        }

        public bool ProxyAllowed
        {
            get
            {
                return this.socks5.ProxyAllowed;
            }
            set
            {
                this.socks5.ProxyAllowed = value;
            }
        }

        public int Socks5ServerPortFrom
        {
            get
            {
                return this.socks5.ServerPortFrom;
            }
            set
            {
                this.socks5.ServerPortFrom = value;
            }
        }

        public int Socks5ServerPortTo
        {
            get
            {
                return this.socks5.ServerPortTo;
            }
            set
            {
                this.socks5.ServerPortTo = value;
            }
        }

        public DnsEndPoint StunServer
        {
            get
            {
                return this.socks5.StunServer;
            }
            set
            {
                this.socks5.StunServer = value;
            }
        }

        public bool UseUPnP
        {
            get
            {
                return this.socks5.UseUPnP;
            }
            set
            {
                this.socks5.UseUPnP = value;
            }
        }
    }
}

