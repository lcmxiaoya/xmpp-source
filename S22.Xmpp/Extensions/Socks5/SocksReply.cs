namespace S22.Xmpp.Extensions.Socks5
{
    using S22.Xmpp;
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Text;

    [Serializable]
    internal class SocksReply
    {
        private const byte version = 5;

        public SocksReply(ReplyStatus status, IPAddress address, ushort port)
        {
            address.ThrowIfNull<IPAddress>("address");
            this.Status = status;
            this.ATyp = (address.AddressFamily == AddressFamily.InterNetworkV6) ? S22.Xmpp.Extensions.Socks5.ATyp.IPv6 : S22.Xmpp.Extensions.Socks5.ATyp.IPv4;
            this.Address = address;
            this.Port = port;
        }

        public SocksReply(ReplyStatus status, string domain, ushort port)
        {
            domain.ThrowIfNull<string>("domain");
            if (domain.Length > 0xff)
            {
                throw new ArgumentException("The length of the domain string must not exceed 255 characters.");
            }
            this.Status = status;
            this.ATyp = S22.Xmpp.Extensions.Socks5.ATyp.Domain;
            this.Address = domain;
            this.Port = port;
        }

        public static SocksReply Deserialize(byte[] buffer)
        {
            SocksReply reply;
            buffer.ThrowIfNull<byte[]>("buffer");
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadByte() != 5)
                    {
                        throw new SerializationException("Invalid SOCKS5 reply.");
                    }
                    ReplyStatus status = (ReplyStatus) reader.ReadByte();
                    reader.ReadByte();
                    S22.Xmpp.Extensions.Socks5.ATyp typ = (S22.Xmpp.Extensions.Socks5.ATyp) reader.ReadByte();
                    IPAddress address = null;
                    string domain = null;
                    switch (typ)
                    {
                        case S22.Xmpp.Extensions.Socks5.ATyp.IPv4:
                        case S22.Xmpp.Extensions.Socks5.ATyp.IPv6:
                            address = new IPAddress(reader.ReadBytes((typ == S22.Xmpp.Extensions.Socks5.ATyp.IPv4) ? 4 : 0x10));
                            break;

                        case S22.Xmpp.Extensions.Socks5.ATyp.Domain:
                        {
                            byte count = reader.ReadByte();
                            domain = Encoding.ASCII.GetString(reader.ReadBytes(count));
                            break;
                        }
                    }
                    ushort port = reader.ReadUInt16(true);
                    if (typ == S22.Xmpp.Extensions.Socks5.ATyp.Domain)
                    {
                        return new SocksReply(status, domain, port);
                    }
                    reply = new SocksReply(status, address, port);
                }
            }
            return reply;
        }

        public byte[] Serialize()
        {
            byte[] addr;
            if (Address is IPAddress)
                addr = (Address as IPAddress).GetAddressBytes();
            else
            {
                byte[] domainBytes = Encoding.ASCII.GetBytes((string)Address);
                addr = new byte[domainBytes.Length + 1];
                addr[0] = Convert.ToByte(domainBytes.Length);
                for (int i = 0; i < domainBytes.Length; i++)
                    addr[1 + i] = domainBytes[i];
            }
            return new ByteBuilder()
                .Append(version)
                .Append((byte)Status)
                .Append((byte)0x00)
                .Append((byte)ATyp)
                .Append(addr)
                .Append(Port, bigEndian: true)
                .ToArray();
        }

        public object Address { get; private set; }

        public S22.Xmpp.Extensions.Socks5.ATyp ATyp { get; private set; }

        public ushort Port { get; private set; }

        public ReplyStatus Status { get; private set; }
    }
}

