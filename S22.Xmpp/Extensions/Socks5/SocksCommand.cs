namespace S22.Xmpp.Extensions.Socks5
{
    using System;

    internal enum SocksCommand : byte
    {
        Bind = 2,
        Connect = 1,
        UdpAssociate = 3
    }
}

