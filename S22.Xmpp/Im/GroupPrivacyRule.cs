namespace S22.Xmpp.Im
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public class GroupPrivacyRule : PrivacyRule
    {
        public GroupPrivacyRule(string group, bool allow, uint order, PrivacyGranularity granularity = 0) : base(allow, order, granularity)
        {
            group.ThrowIfNull<string>("group");
            this.Group = group;
        }

        public string Group { get; private set; }
    }
}

