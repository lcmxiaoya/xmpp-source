namespace S22.Xmpp.Extensions.Socks5
{
    using S22.Xmpp;
    using System;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;

    internal static class TcpListenerExtensions
    {
        public static TcpClient AcceptTcpClient(this TcpListener listener, int timeout)
        {
            if (timeout == -1)
            {
                return listener.AcceptTcpClient();
            }
            timeout.ThrowIfOutOfRange("timeout", 0, 0x7fffffff);
            IAsyncResult asyncResult = listener.BeginAcceptTcpClient(null, null);
            if (!asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds((double) timeout)))
            {
                throw new TimeoutException("The operation timed out.");
            }
            return listener.EndAcceptTcpClient(asyncResult);
        }
    }
}

