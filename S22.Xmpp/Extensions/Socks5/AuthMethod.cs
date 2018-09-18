namespace S22.Xmpp.Extensions.Socks5
{
    using System;

    internal enum AuthMethod : byte
    {
        Gssapi = 1,
        None = 0,
        Username = 2
    }
}

