namespace S22.Xmpp.Im
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public class SubscriptionPrivacyRule : PrivacyRule
    {
        public SubscriptionPrivacyRule(S22.Xmpp.Im.SubscriptionState state, bool allow, uint order, PrivacyGranularity granularity = 0) : base(allow, order, granularity)
        {
            this.SubscriptionState = state;
        }

        public S22.Xmpp.Im.SubscriptionState SubscriptionState { get; private set; }
    }
}

