namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public class JidPrivacyRule : PrivacyRule
    {
        public JidPrivacyRule(S22.Xmpp.Jid jid, bool allow, uint order, PrivacyGranularity granularity = 0) : base(allow, order, granularity)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
        }

        public S22.Xmpp.Jid Jid { get; private set; }
    }
}

