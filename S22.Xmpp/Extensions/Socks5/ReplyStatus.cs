namespace S22.Xmpp.Extensions.Socks5
{
    using System;

    internal enum ReplyStatus : byte
    {
        AddressTypeNotSupported = 8,
        CommandNotSupported = 7,
        ConnectionNotAllowed = 2,
        ConnectionRefused = 5,
        GeneralServerFailure = 1,
        HostUnreachable = 4,
        NetworkUnreachable = 3,
        Succeeded = 0,
        TtlExpired = 6
    }
}

