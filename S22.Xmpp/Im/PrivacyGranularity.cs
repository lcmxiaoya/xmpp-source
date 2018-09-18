namespace S22.Xmpp.Im
{
    using System;

    [Flags]
    public enum PrivacyGranularity
    {
        Iq = 2,
        Message = 1,
        PresenceIn = 4,
        PresenceOut = 8
    }
}

