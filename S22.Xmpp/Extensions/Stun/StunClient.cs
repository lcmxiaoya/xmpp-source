namespace S22.Xmpp.Extensions.Stun
{
    using S22.Xmpp;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;

    internal static class StunClient
    {
        private const int connectionTimeout = 0x274c;
        private const int initialRto = 500;
        private const int rc = 7;
        private const int rm = 0x10;

        public static IPAddress Query(IPAddress address, int port = 0xd96, int timeout = 0x7fffffff)
        {
            address.ThrowIfNull<IPAddress>("address");
            port.ThrowIfOutOfRange("port", 0, 0xffff);
            IPEndPoint endPoint = new IPEndPoint(address, port);
            byte[] dgram = new BindingRequest(null).Serialize();
            int num = 500;
            using (UdpClient client = new UdpClient())
            {
                for (int i = 0; i < 7; i++)
                {
                    client.Send(dgram, dgram.Length, endPoint);
                    client.Client.ReceiveTimeout = num;
                    try
                    {
                        return BindingResponse.Deserialize(client.Receive(ref endPoint)).Address;
                    }
                    catch (SocketException exception)
                    {
                        if (exception.ErrorCode != 0x274c)
                        {
                            throw;
                        }
                        timeout -= num;
                        if (timeout <= 0)
                        {
                            throw new TimeoutException("The timeout has expired.");
                        }
                    }
                    catch (SerializationException)
                    {
                        throw new ProtocolViolationException("The STUN Binding Response is invalid.");
                    }
                    if (i < 6)
                    {
                        num *= 2;
                    }
                    else
                    {
                        num = 0x1f40;
                    }
                    if (timeout < num)
                    {
                        num = timeout;
                    }
                }
                throw new SocketException(0x274c);
            }
        }

        public static IPAddress Query(string host, int port = 0xd96, int timeout = 0x7fffffff)
        {
            host.ThrowIfNull<string>("host");
            port.ThrowIfOutOfRange("port", 0, 0xffff);
            IPAddress[] hostAddresses = Dns.GetHostAddresses(host);
            IPAddress address = hostAddresses.FirstOrDefault<IPAddress>(a => a.AddressFamily == AddressFamily.InterNetwork);
            if (address == null)
            {
                address = hostAddresses[0];
            }
            return Query(address, port, timeout);
        }
    }
}

