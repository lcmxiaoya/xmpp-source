namespace S22.Xmpp.Im
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    public class Status
    {
        public Status(S22.Xmpp.Im.Availability availability, Dictionary<string, string> messages, sbyte priority = 0)
        {
            this.Availability = availability;
            this.Priority = priority;
            this.Messages = new Dictionary<string, string>();
            if (messages != null)
            {
                foreach (KeyValuePair<string, string> pair in messages)
                {
                    this.Messages.Add(pair.Key, pair.Value);
                }
            }
        }

        public Status(S22.Xmpp.Im.Availability availability, string message = null, sbyte priority = 0, CultureInfo language = null)
        {
            this.Availability = availability;
            this.Priority = priority;
            this.Messages = new Dictionary<string, string>();
            if (language == null)
            {
                language = CultureInfo.CurrentCulture;
            }
            if (message != null)
            {
                this.Messages.Add(language.Name, message);
            }
        }

        public S22.Xmpp.Im.Availability Availability { get; private set; }

        public string Message
        {
            get
            {
                return this.Messages.Values.FirstOrDefault<string>();
            }
        }

        public Dictionary<string, string> Messages { get; private set; }

        public sbyte Priority { get; private set; }
    }
}

