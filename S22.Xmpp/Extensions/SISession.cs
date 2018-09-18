namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;

    [Serializable]
    internal class SISession
    {
        public SISession(string sid, System.IO.Stream stream, long size, bool receiving, Jid from, Jid to, IDataStream extension)
        {
            sid.ThrowIfNull<string>("sid");
            stream.ThrowIfNull<System.IO.Stream>("stream");
            size.ThrowIfOutOfRange(0L, 0x7fffffffffffffffL);
            from.ThrowIfNull<Jid>("from");
            to.ThrowIfNull<Jid>("to");
            extension.ThrowIfNull<IDataStream>("extension");
            if (!(!receiving || stream.CanWrite))
            {
                throw new ArgumentException("The specified stream cannot be written.");
            }
            if (!(receiving || stream.CanRead))
            {
                throw new ArgumentException("The specified stream cannot be read.");
            }
            this.Sid = sid;
            this.Stream = stream;
            this.Size = size;
            this.Count = 0L;
            this.Receiving = receiving;
            this.From = from;
            this.To = to;
            this.Extension = extension;
        }

        public long Count { get; set; }

        public IDataStream Extension { get; private set; }

        public Jid From { get; private set; }

        public bool Receiving { get; private set; }

        public string Sid { get; private set; }

        public long Size { get; private set; }

        public System.IO.Stream Stream { get; private set; }

        public Jid To { get; private set; }
    }
}

