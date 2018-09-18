namespace S22.Xmpp.Extensions.Socks5
{
    using S22.Xmpp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;

    [Serializable]
    internal class ClientGreeting
    {
        private HashSet<AuthMethod> methods;
        private const byte version = 5;

        public ClientGreeting(params AuthMethod[] methods)
        {
            this.methods = new HashSet<AuthMethod>();
            if (methods != null)
            {
                foreach (AuthMethod method in methods)
                {
                    this.methods.Add(method);
                }
            }
        }

        public ClientGreeting(IEnumerable<AuthMethod> methods)
        {
            this.methods = new HashSet<AuthMethod>();
            methods.ThrowIfNull<IEnumerable<AuthMethod>>("methods");
            foreach (AuthMethod method in methods)
            {
                this.methods.Add(method);
            }
        }

        public static ClientGreeting Deserialize(byte[] buffer)
        {
            ClientGreeting greeting;
            buffer.ThrowIfNull<byte[]>("buffer");
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadByte() != 5)
                    {
                        throw new SerializationException("Invalid SOCKS5 greeting.");
                    }
                    byte num = reader.ReadByte();
                    AuthMethod[] methods = new AuthMethod[num];
                    for (int i = 0; i < num; i++)
                    {
                        methods[i] = (AuthMethod) reader.ReadByte();
                    }
                    greeting = new ClientGreeting(methods);
                }
            }
            return greeting;
        }

        public byte[] Serialize()
        {
            ByteBuilder b = new ByteBuilder()
                .Append(version)
                .Append((byte)methods.Count);
            foreach (AuthMethod m in Methods)
                b.Append((byte)m);
            return b.ToArray();
        }

        public IEnumerable<AuthMethod> Methods
        {
            get
            {
                return this.methods;
            }
        }
    }
}

