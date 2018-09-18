namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    [Serializable]
    public class Streamhost
    {
        public Streamhost(S22.Xmpp.Jid jid, string host, int port)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            host.ThrowIfNullOrEmpty("host");
            port.ThrowIfOutOfRange("port", 0, 0xffff);
            this.Jid = jid;
            this.Host = host;
            this.Port = port;
        }

        public string Host { get; private set; }

        public S22.Xmpp.Jid Jid { get; private set; }

        public int Port { get; private set; }
    }
}

