namespace S22.Xmpp.Extensions
{
    using S22.Xmpp.Core;
    using System;

    internal interface IInputFilter<T> where T: Stanza
    {
        bool Input(T stanza);
    }
}

