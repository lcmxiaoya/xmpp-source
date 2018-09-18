namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml;

    internal class EntityCapabilities : XmppExtension, IInputFilter<Presence>, IOutputFilter<Presence>
    {
        private IDictionary<string, IEnumerable<Extension>> cachedFeatures;
        private IDictionary<Jid, string> hashes;
        private ServiceDiscovery sdisco;

        public EntityCapabilities(XmppIm im) : base(im)
        {
            this.hashes = new Dictionary<Jid, string>();
            this.cachedFeatures = new Dictionary<string, IEnumerable<Extension>>();
        }

        private string GenerateVerificationString()
        {
            Identity identity = this.sdisco.Identity;
            StringBuilder builder = new StringBuilder(identity.Category + "/" + identity.Type + "//" + identity.Name + "<");
            List<string> list = new List<string>(this.sdisco.Features);
            list.Sort();
            foreach (string str in list)
            {
                builder.Append(str + "<");
            }
            byte[] bytes = Encoding.UTF8.GetBytes(builder.ToString());
            using (SHA1Managed managed = new SHA1Managed())
            {
                return Convert.ToBase64String(managed.ComputeHash(bytes));
            }
        }

        public IEnumerable<Extension> GetExtensions(Jid jid)
        {
            jid.ThrowIfNull<Jid>("jid");
            if (this.hashes.ContainsKey(jid))
            {
                string key = this.hashes[jid];
                if (!this.cachedFeatures.ContainsKey(key))
                {
                    this.cachedFeatures.Add(key, this.sdisco.GetExtensions(jid));
                }
                return this.cachedFeatures[key];
            }
            return this.sdisco.GetExtensions(jid);
        }

        public IEnumerable<Identity> GetIdentities(Jid jid)
        {
            return this.sdisco.GetIdentities(jid);
        }

        private string Hash(string input, HashAlgorithm algorithm)
        {
            input.ThrowIfNull<string>("input");
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(algorithm.ComputeHash(bytes));
        }

        public bool HasIdentity(Jid jid, string category, string type)
        {
            jid.ThrowIfNull<Jid>("jid");
            category.ThrowIfNull<string>("category");
            type.ThrowIfNull<string>("type");
            foreach (Identity identity in this.GetIdentities(jid))
            {
                if ((identity.Category == category) && (identity.Type == type))
                {
                    return true;
                }
            }
            return false;
        }

        public override void Initialize()
        {
            this.sdisco = base.im.GetExtension<ServiceDiscovery>();
        }

        public bool Input(Presence stanza)
        {
            XmlElement element = stanza.Data["c"];
            if ((element != null) && (element.NamespaceURI == "http://jabber.org/protocol/caps"))
            {
                string attribute = element.GetAttribute("hash");
                string str2 = element.GetAttribute("ver");
                string str3 = element.GetAttribute("node");
                if (string.IsNullOrEmpty(attribute) || string.IsNullOrWhiteSpace(str2))
                {
                    return false;
                }
                this.hashes[stanza.From] = str2;
            }
            return false;
        }

        public void Output(Presence stanza)
        {
            XmlElement child = Xml.Element("c", "http://jabber.org/protocol/caps").Attr("hash", "sha-1").Attr("node", this.nodeUri).Attr("ver", this.GenerateVerificationString());
            stanza.Data.Child(child);
        }

        private HashAlgorithm ParseHashAlgorithm(string algorithm)
        {
            algorithm.ThrowIfNull<string>("algorithm");
            Dictionary<string, Func<HashAlgorithm>> dictionary2 = new Dictionary<string, Func<HashAlgorithm>>(StringComparer.InvariantCultureIgnoreCase);
            dictionary2.Add("sha-1", () => new SHA1Managed());
            dictionary2.Add("sha-256", () => new SHA256Managed());
            dictionary2.Add("sha-384", () => new SHA384Managed());
            dictionary2.Add("sha-512", () => new SHA512Managed());
            dictionary2.Add("md5", () => new MD5CryptoServiceProvider());
            Dictionary<string, Func<HashAlgorithm>> dictionary = dictionary2;
            return (dictionary.ContainsKey(algorithm) ? dictionary[algorithm]() : null);
        }

        public bool Supports<T>(Jid jid) where T: XmppExtension
        {
            jid.ThrowIfNull<Jid>("jid");
            T extension = base.im.GetExtension<T>();
            return this.Supports(jid, new Extension[] { extension.Xep });
        }

        public bool Supports(Jid jid, params Extension[] extensions)
        {
            jid.ThrowIfNull<Jid>("jid");
            extensions.ThrowIfNull<Extension[]>("extensions");
            IEnumerable<Extension> source = this.GetExtensions(jid);
            foreach (Extension extension in extensions)
            {
                if (!source.Contains<Extension>(extension))
                {
                    return false;
                }
            }
            return true;
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "http://jabber.org/protocol/caps" };
            }
        }

        private string nodeUri
        {
            get
            {
                return "S22.Xmpp";
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.EntityCapabilities;
            }
        }
    }
}

