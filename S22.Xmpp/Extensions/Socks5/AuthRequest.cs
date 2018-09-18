namespace S22.Xmpp.Extensions.Socks5
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Text;

    [Serializable]
    internal class AuthRequest
    {
        private const byte version = 1;

        public AuthRequest(string username, string password)
        {
            username.ThrowIfNull<string>("username");
            password.ThrowIfNull<string>("password");
            this.Username = username;
            this.Password = password;
        }

        public byte[] Serialize()
        {
            byte[] bytes = Encoding.ASCII.GetBytes(this.Username);
            byte[] values = Encoding.ASCII.GetBytes(this.Password);
            return new ByteBuilder().Append(new byte[] { 1 }).Append(new byte[] { Convert.ToByte(bytes.Length) }).Append(bytes).Append(new byte[] { Convert.ToByte(values.Length) }).Append(values).ToArray();
        }

        public string Password { get; private set; }

        public string Username { get; private set; }
    }
}

