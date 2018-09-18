namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Xml;

    internal class InitiationResult
    {
        public InitiationResult(string sessionId, string method, XmlElement data = null)
        {
            sessionId.ThrowIfNull<string>("sessionId");
            method.ThrowIfNull<string>("method");
            this.SessionId = sessionId;
            this.Method = method;
            this.Data = data;
        }

        public XmlElement Data { get; private set; }

        public string Method { get; private set; }

        public string SessionId { get; private set; }
    }
}

