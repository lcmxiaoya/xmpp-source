namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;

    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(System.Exception e)
        {
            e.ThrowIfNull<System.Exception>("e");
            this.Exception = e;
        }

        public System.Exception Exception { get; private set; }

        public string Reason
        {
            get
            {
                return this.Exception.Message;
            }
        }
    }
}

