namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    public class ActivityChangedEventArgs : EventArgs
    {
        public ActivityChangedEventArgs(S22.Xmpp.Jid jid, GeneralActivity activity, SpecificActivity specific =  SpecificActivity.Other, string description = null)
        {
            jid.ThrowIfNull<S22.Xmpp.Jid>("jid");
            this.Jid = jid;
            this.Activity = activity;
            this.Specific = specific;
            this.Description = description;
        }

        public GeneralActivity Activity { get; private set; }

        public string Description { get; private set; }

        public S22.Xmpp.Jid Jid { get; private set; }

        public SpecificActivity Specific { get; private set; }
    }
}

