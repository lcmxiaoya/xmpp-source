﻿namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    [Serializable]
    internal class TransferAbortedEventArgs : EventArgs
    {
        public TransferAbortedEventArgs(SISession session)
        {
            session.ThrowIfNull<SISession>("session");
            this.Session = session;
        }

        public SISession Session { get; private set; }
    }
}

