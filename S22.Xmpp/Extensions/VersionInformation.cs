namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    public class VersionInformation
    {
        public VersionInformation(string name, string version, string os = null)
        {
            name.ThrowIfNull<string>("name");
            version.ThrowIfNull<string>("version");
            this.Name = name;
            this.Version = version;
            this.Os = os;
        }

        public string Name { get; private set; }

        public string Os { get; private set; }

        public string Version { get; private set; }
    }
}

