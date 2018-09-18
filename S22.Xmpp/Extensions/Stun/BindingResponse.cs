namespace S22.Xmpp.Extensions.Stun
{
    using S22.Xmpp;
    using S22.Xmpp.Extensions.Socks5;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;

    internal class BindingResponse
    {
        private const int headerSize = 20;
        private const byte IPv4 = 1;
        private const byte IPv6 = 2;
        private static byte[] magicCookie = new byte[] { 0x21, 0x12, 0xa4, 0x42 };
        private const short mappedAddress = 0x100;
        private const short stunMessageType = 0x101;
        private const short xorMappedAddress = 0x2000;

        internal BindingResponse(byte[] id, IPAddress address)
        {
            id.ThrowIfNull<byte[]>("id");
            address.ThrowIfNull<IPAddress>("address");
            this.Id = id;
            this.Address = address;
        }

        public static BindingResponse Deserialize(byte[] buffer)
        {
            buffer.ThrowIfNull<byte[]>("buffer");
            if (buffer.Length < 20)
            {
                throw new SerializationException("The buffer does not contain a valid STUN message header.");
            }
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt16() != 0x101)
                    {
                        throw new SerializationException("Unexpected STUN message type.");
                    }
                    int num = reader.ReadInt16(true);
                    if (!reader.ReadBytes(4).SequenceEqual<byte>(magicCookie))
                    {
                        throw new SerializationException("Invalid 'Magic Cookie' value.");
                    }
                    byte[] values = reader.ReadBytes(12);
                    try
                    {
                        while (num > 0)
                        {
                            short num2 = reader.ReadInt16();
                            short count = reader.ReadInt16(true);
                            switch (num2)
                            {
                                case 0x100:
                                case 0x2000:
                                {
                                    reader.ReadByte();
                                    byte num4 = reader.ReadByte();
                                    if ((num4 != 1) && (num4 != 2))
                                    {
                                        throw new SerializationException("Invalid address-family.");
                                    }
                                    short num5 = reader.ReadInt16();
                                    byte[] a = reader.ReadBytes((num4 == 1) ? 4 : 0x10);
                                    if (num2 == 0x2000)
                                    {
                                        a = Xor(a, (num4 == 1) ? magicCookie : new ByteBuilder().Append(magicCookie).Append(values).ToArray());
                                    }
                                    return new BindingResponse(values, new IPAddress(a));
                                }
                            }
                            reader.ReadBytes(count);
                            num = (num - 4) - count;
                        }
                    }
                    catch (Exception exception)
                    {
                        throw new SerializationException("The format of the STUN binding response is invalid.", exception);
                    }
                    throw new SerializationException("+The binding response does not contain  a MAPPED-ADDRESS attribute.");
                }
            }
        }

        private static byte[] Xor(byte[] a, byte[] b)
        {
            a.ThrowIfNull<byte[]>("a");
            b.ThrowIfNull<byte[]>("b");
            if (a.Length != b.Length)
            {
                throw new ArgumentException("The input arrays must have the same number of elements.");
            }
            byte[] buffer = new byte[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                buffer[i] = (byte) (a[i] ^ b[i]);
            }
            return buffer;
        }

        public IPAddress Address { get; private set; }

        public byte[] Id { get; private set; }
    }
}

