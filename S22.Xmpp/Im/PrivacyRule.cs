namespace S22.Xmpp.Im
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public class PrivacyRule
    {
        public PrivacyRule(bool allow, uint order, PrivacyGranularity granularity = 0)
        {
            this.Allow = allow;
            this.Order = order;
            this.Granularity = granularity;
        }

        public bool Allow { get; private set; }

        public PrivacyGranularity Granularity { get; private set; }

        public uint Order { get; set; }
    }
}

