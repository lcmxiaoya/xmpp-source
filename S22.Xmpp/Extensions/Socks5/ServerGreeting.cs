namespace S22.Xmpp.Extensions.Socks5
{
    using S22.Xmpp;
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;

    [Serializable]
    internal class ServerGreeting
    {
        private const byte version = 5;

        public ServerGreeting(AuthMethod method)
        {
            this.Method = method;
        }

        public static ServerGreeting Deserialize(byte[] buffer)
        {
            ServerGreeting greeting;
            buffer.ThrowIfNull<byte[]>("buffer");
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadByte() != 5)
                    {
                        throw new SerializationException("Invalid SOCKS5 greeting.");
                    }
                    greeting = new ServerGreeting((AuthMethod) reader.ReadByte());
                }
            }
            return greeting;
        }

        public byte[] Serialize()
        {
            return new ByteBuilder()
                .Append(version)
                .Append((byte)Method)
                .ToArray();
        }

        public AuthMethod Method { get; private set; }
    }
}

