namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    internal class Identity
    {
        public Identity(string category, string type, string name = null)
        {
            category.ThrowIfNull<string>("category");
            type.ThrowIfNull<string>("type");
            this.Category = category;
            this.Type = type;
            this.Name = name;
        }

        public string Category { get; private set; }

        public string Name { get; private set; }

        public string Type { get; private set; }
    }
}

