namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Net;
    using System.Runtime.CompilerServices;

    internal static class IPAddressExtensions
    {
        private static IPAddress And(this IPAddress address, IPAddress netmask)
        {
            netmask.ThrowIfNull<IPAddress>("netmask");
            if (address.AddressFamily != netmask.AddressFamily)
            {
                throw new ArgumentException("The address family of the specified netmask is different from the address family of the IP address.");
            }
            byte[] addressBytes = address.GetAddressBytes();
            byte[] buffer2 = netmask.GetAddressBytes();
            for (int i = 0; i < addressBytes.Length; i++)
            {
                addressBytes[i] = (byte) (addressBytes[i] & buffer2[i]);
            }
            return new IPAddress(addressBytes);
        }

        public static bool InSameSubnet(this IPAddress address, IPAddress other, IPAddress netmask)
        {
            other.ThrowIfNull<IPAddress>("other");
            netmask.ThrowIfNull<IPAddress>("netmask");
            return address.And(netmask).Equals(other.And(netmask));
        }
    }
}

