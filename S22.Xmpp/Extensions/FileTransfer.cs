namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    public class FileTransfer
    {
        internal FileTransfer(SISession session, string name, string description) : this(session.From, session.To, name, session.Size, session.Sid, description, session.Count)
        {
        }

        internal FileTransfer(Jid from, Jid to, string name, long size, string sessionId = null, string description = null, long transferred = 0L)
        {
            from.ThrowIfNull<Jid>("from");
            to.ThrowIfNull<Jid>("to");
            name.ThrowIfNull<string>("name");
            size.ThrowIfOutOfRange("size", 0L, 0x7fffffffffffffffL);
            transferred.ThrowIfOutOfRange("transferred", 0L, size);
            this.From = from;
            this.To = to;
            this.Name = name;
            this.Size = size;
            this.SessionId = sessionId;
            this.Description = description;
            this.Transferred = transferred;
        }

        public string Description { get; private set; }

        public Jid From { get; private set; }

        public string Name { get; private set; }

        public string SessionId { get; private set; }

        public long Size { get; private set; }

        public Jid To { get; private set; }

        public long Transferred { get; private set; }
    }
}

