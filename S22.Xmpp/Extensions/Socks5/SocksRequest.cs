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
    internal class SocksRequest
    {
        private const byte version = 5;

        public SocksRequest(SocksCommand command, IPAddress destination, ushort port)
        {
            destination.ThrowIfNull<IPAddress>("destination");
            this.Command = command;
            this.ATyp = (destination.AddressFamily == AddressFamily.InterNetworkV6) ? S22.Xmpp.Extensions.Socks5.ATyp.IPv6 : S22.Xmpp.Extensions.Socks5.ATyp.IPv4;
            this.Destination = destination;
            this.Port = port;
        }

        public SocksRequest(SocksCommand command, string domain, ushort port)
        {
            domain.ThrowIfNull<string>("domain");
            if (domain.Length > 0xff)
            {
                throw new ArgumentException("The length of the domain string must not exceed 255 characters.");
            }
            this.Command = command;
            this.ATyp = S22.Xmpp.Extensions.Socks5.ATyp.Domain;
            this.Destination = domain;
            this.Port = port;
        }

        public static SocksRequest Deserialize(byte[] buffer)
        {
            SocksRequest request;
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadByte() != 5)
                    {
                        throw new SerializationException("Invalid SOCKS5 request.");
                    }
                    SocksCommand command = (SocksCommand) reader.ReadByte();
                    reader.ReadByte();
                    S22.Xmpp.Extensions.Socks5.ATyp typ = (S22.Xmpp.Extensions.Socks5.ATyp) reader.ReadByte();
                    IPAddress destination = null;
                    string domain = null;
                    switch (typ)
                    {
                        case S22.Xmpp.Extensions.Socks5.ATyp.IPv4:
                        case S22.Xmpp.Extensions.Socks5.ATyp.IPv6:
                            destination = new IPAddress(reader.ReadBytes((typ == S22.Xmpp.Extensions.Socks5.ATyp.IPv4) ? 4 : 0x10));
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
                        return new SocksRequest(command, domain, port);
                    }
                    request = new SocksRequest(command, destination, port);
                }
            }
            return request;
        }

        public byte[] Serialize()
        {
            byte[] dest;
            if (Destination is IPAddress)
                dest = (Destination as IPAddress).GetAddressBytes();
            else
            {
                byte[] domainBytes = Encoding.ASCII.GetBytes((string)Destination);
                dest = new byte[domainBytes.Length + 1];
                dest[0] = Convert.ToByte(domainBytes.Length);
                for (int i = 0; i < domainBytes.Length; i++)
                    dest[1 + i] = domainBytes[i];
            }
            return new ByteBuilder()
                .Append(version)
                .Append((byte)Command)
                .Append((byte)0x00)
                .Append((byte)ATyp)
                .Append(dest)
                .Append(Port, bigEndian: true)
                .ToArray();
        }

        public S22.Xmpp.Extensions.Socks5.ATyp ATyp { get; private set; }

        public SocksCommand Command { get; private set; }

        public object Destination { get; private set; }

        public ushort Port { get; private set; }
    }
}

