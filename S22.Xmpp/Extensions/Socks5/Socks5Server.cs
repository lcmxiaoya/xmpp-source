namespace S22.Xmpp.Extensions.Socks5
{
    using S22.Xmpp;
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class Socks5Server : IDisposable
    {
        private TcpClient client;
        private bool disposed;
        private TcpListener listener;
        private const int receiveTimeout = 0x2710;
        private NetworkStream stream;

        public Socks5Server(int port, IPAddress localaddress = null)
        {
            if (localaddress == null)
            {
                localaddress = IPAddress.Any;
            }
            this.listener = new TcpListener(localaddress, port);
            this.listener.Start();
            this.Port = port;
        }

        public SocksRequest Accept(int timeout = -1)
        {
            this.AssertValid();
            this.client = this.listener.AcceptTcpClient(timeout);
            this.client.ReceiveTimeout = 0x2710;
            this.InitializeConnection();
            return this.WaitForRequest();
        }

        private void AssertValid()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(base.GetType().FullName);
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
            this.PerformGreeting();
        }

        private void PerformGreeting()
        {
            ByteBuilder builder = new ByteBuilder();
            using (BinaryReader reader = new BinaryReader(this.stream, Encoding.UTF8))
            {
                byte[] values = reader.ReadBytes(2);
                builder.Append(values);
                builder.Append(reader.ReadBytes(values[1]));
            }
            if (!ClientGreeting.Deserialize(builder.ToArray()).Methods.Contains<AuthMethod>(AuthMethod.None))
            {
                this.Dispose();
                throw new Socks5Exception("Client requires authentication.");
            }
            byte[] buffer2 = new ServerGreeting(AuthMethod.None).Serialize();
            this.stream.Write(buffer2, 0, buffer2.Length);
        }

        public void Reply(SocksReply reply)
        {
            reply.ThrowIfNull<SocksReply>("reply");
            this.AssertValid();
            byte[] buffer = reply.Serialize();
            this.stream.Write(buffer, 0, buffer.Length);
        }

        public void Reply(ReplyStatus status, IPAddress address, ushort port)
        {
            this.Reply(new SocksReply(status, address, port));
        }

        public void Reply(ReplyStatus status, string domain, ushort port)
        {
            this.Reply(new SocksReply(status, domain, port));
        }

        private SocksRequest WaitForRequest()
        {
            SocksRequest request;
            ByteBuilder builder = new ByteBuilder();
            using (BinaryReader reader = new BinaryReader(this.stream, Encoding.UTF8))
            {
                byte[] values = reader.ReadBytes(4);
                builder.Append(values);
                ATyp typ = (ATyp) values[3];
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
            }
            try
            {
                request = SocksRequest.Deserialize(builder.ToArray());
            }
            catch (Exception exception)
            {
                throw new Socks5Exception("The request could not be serialized.", exception);
            }
            return request;
        }

        public int Port { get; private set; }
    }
}

