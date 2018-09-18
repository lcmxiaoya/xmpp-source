namespace S22.Xmpp.Extensions.Socks5
{
    using S22.Xmpp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class Socks5Client : IDisposable
    {
        private TcpClient client;
        private bool disposed;
        private NetworkStream stream;

        public Socks5Client(IPAddress address, int port, string username = null, string password = null)
        {
            address.ThrowIfNull<IPAddress>("address");
            port.ThrowIfOutOfRange("port", 0, 0xffff);
            this.Username = username;
            this.Password = password;
            this.client = new TcpClient();
            this.client.Connect(address, port);
            this.InitializeConnection();
        }

        public Socks5Client(string hostname, int port, string username = null, string password = null)
        {
            hostname.ThrowIfNull<string>("hostname");
            port.ThrowIfOutOfRange("port", 0, 0xffff);
            this.Username = username;
            this.Password = password;
            this.client = new TcpClient(hostname, port);
            this.InitializeConnection();
        }

        private void AssertValid()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
            }
        }

        private void Authenticate()
        {
            byte[] buffer = new AuthRequest(this.Username, this.Password).Serialize();
            this.stream.Write(buffer, 0, buffer.Length);
            buffer = new byte[2];
            this.stream.Read(buffer, 0, 2);
            if (!AuthResponse.Deserialize(buffer).Success)
            {
                throw new Socks5Exception("Authentication failed.");
            }
        }

        public void Close()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    if (this.stream != null)
                    {
                        this.stream.Dispose();
                    }
                    this.stream = null;
                    if (this.client != null)
                    {
                        this.client.Close();
                    }
                    this.client = null;
                }
            }
        }

        public NetworkStream GetStream()
        {
            this.AssertValid();
            return this.stream;
        }

        private void InitializeConnection()
        {
            this.stream = this.client.GetStream();
            switch (this.PerformGreeting().Method)
            {
                case AuthMethod.None:
                    return;

                case AuthMethod.Username:
                    this.Authenticate();
                    return;
            }
            throw new Socks5Exception("No acceptable authentication method.");
        }

        private ServerGreeting PerformGreeting()
        {
            HashSet<AuthMethod> methods = new HashSet<AuthMethod> { 0 };
            if (!string.IsNullOrEmpty(this.Username))
            {
                methods.Add(AuthMethod.Username);
            }
            byte[] buffer = new ClientGreeting(methods).Serialize();
            this.stream.Write(buffer, 0, buffer.Length);
            buffer = new byte[2];
            this.stream.Read(buffer, 0, 2);
            return ServerGreeting.Deserialize(buffer);
        }

        public SocksReply Request(SocksRequest request)
        {
            SocksReply reply;
            request.ThrowIfNull<SocksRequest>("request");
            this.AssertValid();
            try
            {
                byte[] buffer = request.Serialize();
                this.stream.Write(buffer, 0, buffer.Length);
                ByteBuilder builder = new ByteBuilder();
                BinaryReader reader = new BinaryReader(this.stream, Encoding.UTF8);
                buffer = reader.ReadBytes(4);
                builder.Append(buffer);
                ATyp typ = (ATyp) buffer[3];
                switch (typ)
                {
                    case ATyp.IPv4:
                    case ATyp.IPv6:
                        builder.Append(reader.ReadBytes((typ == ATyp.IPv4) ? 4 : 0x10));
                        break;

                    case ATyp.Domain:
                    {
                        byte count = reader.ReadByte();
                        builder.Append(new byte[] { count }).Append(reader.ReadBytes(count));
                        break;
                    }
                }
                builder.Append(reader.ReadBytes(2));
                reply = SocksReply.Deserialize(builder.ToArray());
            }
            catch (Exception exception)
            {
                CommonConfig.Logger.WriteError("The request could not be performed.", exception);
                throw new Socks5Exception("The request could not be performed.", exception);
            }
            return reply;
        }

        public SocksReply Request(SocksCommand command, IPAddress address, ushort port)
        {
            return this.Request(new SocksRequest(command, address, port));
        }

        public SocksReply Request(SocksCommand command, string domain, ushort port)
        {
            return this.Request(new SocksRequest(command, domain, port));
        }

        public string Password { get; set; }

        public string Username { get; set; }
    }
}

