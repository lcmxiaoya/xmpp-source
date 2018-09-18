namespace S22.Xmpp.Extensions.Socks5
{
    using S22.Xmpp;
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;

    [Serializable]
    internal class AuthResponse
    {
        private const byte version = 1;

        public AuthResponse(bool success)
        {
            this.Success = success;
        }

        public static AuthResponse Deserialize(byte[] buffer)
        {
            AuthResponse response;
            buffer.ThrowIfNull<byte[]>("buffer");
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadByte() != 1)
                    {
                        throw new SerializationException("Invalid auth response.");
                    }
                    bool success = reader.ReadByte() == 0;
                    response = new AuthResponse(success);
                }
            }
            return response;
        }

        public byte[] Serialize()
        {
            return new ByteBuilder().Append(new byte[] { 1 }).Append(new byte[] { this.Success ? ((byte) 0) : ((byte) 0xff) }).ToArray();
        }

        public bool Success { get; private set; }
    }
}

