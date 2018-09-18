namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml;

    [Serializable]
    internal class BobData
    {
        public BobData(byte[] data, string type)
        {
            data.ThrowIfNull<byte[]>("data");
            type.ThrowIfNull<string>("type");
            this.Data = data;
            this.Type = type;
            this.Cid = "sha1+" + this.Sha1(this.Data) + "@bob.xmpp.org";
        }

        private BobData(byte[] data, string type, string cid)
        {
            data.ThrowIfNull<byte[]>("data");
            type.ThrowIfNull<string>("type");
            cid.ThrowIfNull<string>("cid");
            this.Data = data;
            this.Type = type;
            this.Cid = cid;
        }

        public static BobData Parse(XmlElement data)
        {
            BobData data2;
            if (data.NamespaceURI != "urn:xmpp:bob")
            {
                throw new ArgumentException("Invalid namespace attribute.");
            }
            string attribute = data.GetAttribute("type");
            if (string.IsNullOrEmpty(attribute))
            {
                throw new ArgumentException("The type attribute is missing.");
            }
            string str2 = data.GetAttribute("cid");
            if (string.IsNullOrEmpty(str2))
            {
                throw new ArgumentException("The cid attribute is missing.");
            }
            try
            {
                data2 = new BobData(Convert.FromBase64String(data.InnerText), attribute, str2);
            }
            catch (FormatException exception)
            {
                throw new ArgumentException("Invalid Base64 data.", exception);
            }
            return data2;
        }

        private string Sha1(byte[] data)
        {
            data.ThrowIfNull<byte[]>("data");
            using (SHA1Managed managed = new SHA1Managed())
            {
                byte[] buffer = managed.ComputeHash(data);
                StringBuilder builder = new StringBuilder();
                foreach (byte num in buffer)
                {
                    builder.Append(num.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public override string ToString()
        {
            return Xml.Element("data", "urn:xmpp:bob").Attr("cid", this.Cid).Attr("type", this.Type).Text(Convert.ToBase64String(this.Data)).ToXmlString(false, false);
        }

        public string Cid { get; private set; }

        public byte[] Data { get; private set; }

        public string Type { get; private set; }
    }
}

