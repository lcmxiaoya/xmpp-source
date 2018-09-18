namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    internal class FileMetaData
    {
        public FileMetaData(string name, string description = null)
        {
            name.ThrowIfNull<string>("name");
            this.Name = name;
            this.Description = description;
        }

        public string Description { get; private set; }

        public string Name { get; private set; }
    }
}

