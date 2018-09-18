namespace S22.Xmpp.Extensions.Socks5
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    internal static class BinaryReaderExtensions
    {
        public static short ReadInt16(this BinaryReader reader, bool bigEndian)
        {
            if (!bigEndian)
            {
                return reader.ReadInt16();
            }
            int num = 0;
            num |= reader.ReadByte() << 8;
            num |= reader.ReadByte();
            return (short) num;
        }

        public static ushort ReadUInt16(this BinaryReader reader, bool bigEndian)
        {
            if (!bigEndian)
            {
                return reader.ReadUInt16();
            }
            int num = 0;
            num |= reader.ReadByte() << 8;
            num |= reader.ReadByte();
            return (ushort) num;
        }

        public static uint ReadUInt32(this BinaryReader reader, bool bigEndian)
        {
            if (!bigEndian)
            {
                return reader.ReadUInt32();
            }
            int num = 0;
            num |= reader.ReadByte() << 0x18;
            num |= reader.ReadByte() << 0x10;
            num |= reader.ReadByte() << 8;
            num |= reader.ReadByte();
            return (uint) num;
        }
    }
}

