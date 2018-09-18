namespace S22.Xmpp.Extensions.Stun
{
    using S22.Xmpp.Extensions.Socks5;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;

    internal class BindingRequest
    {
        private static RNGCryptoServiceProvider cryptoProvider = new RNGCryptoServiceProvider();
        private readonly byte[] magicCookie = new byte[] { 0x21, 0x12, 0xa4, 0x42 };
        private const short stunMessageType = 0x100;

        public BindingRequest(byte[] id = null)
        {
            if (id != null)
            {
                if (id.Length != 12)
                {
                    throw new ArgumentException("The id parameter must have a length of 12.");
                }
                this.Id = id;
            }
            else
            {
                this.Id = new byte[12];
                cryptoProvider.GetBytes(this.Id);
            }
        }

        public byte[] Serialize()
        {
            return new ByteBuilder().Append((short) 0x100, false).Append(new byte[2]).Append(this.magicCookie).Append(this.Id).ToArray();
        }

        public byte[] Id { get; private set; }
    }
}

