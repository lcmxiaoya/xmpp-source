namespace S22.Xmpp.Im
{
    using S22.Xmpp.Core;
    using System;

    public class ErrorEventArgs : S22.Xmpp.Core.ErrorEventArgs
    {
        public ErrorEventArgs(Exception e) : base(e)
        {
        }
    }
}

