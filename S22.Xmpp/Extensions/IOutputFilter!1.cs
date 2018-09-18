namespace S22.Xmpp.Extensions
{
    using S22.Xmpp.Core;
    using System;

    internal interface IOutputFilter<T> where T: Stanza
    {
        void Output(T stanza);
    }
}

