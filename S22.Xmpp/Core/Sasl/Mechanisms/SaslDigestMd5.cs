namespace S22.Xmpp.Core.Sasl.Mechanisms
{
    using S22.Xmpp;
    using S22.Xmpp.Core.Sasl;
    using System;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;

    internal class SaslDigestMd5 : SaslMechanism
    {
        private string Cnonce;
        private bool Completed;
        private int Step;

        private SaslDigestMd5()
        {
            this.Completed = false;
            this.Cnonce = GenerateCnonce();
            this.Step = 0;
        }

        public SaslDigestMd5(string username, string password)
        {
            this.Completed = false;
            this.Cnonce = GenerateCnonce();
            this.Step = 0;
            username.ThrowIfNull<string>("username");
            if (username == string.Empty)
            {
                throw new ArgumentException("The username must not be empty.");
            }
            password.ThrowIfNull<string>("password");
            this.Username = username;
            this.Password = password;
        }

        internal SaslDigestMd5(string username, string password, string cnonce) : this(username, password)
        {
            this.Cnonce = cnonce;
        }

        private byte[] ComputeDigestResponse(byte[] challenge)
        {
            if (string.IsNullOrEmpty(this.Username) || (this.Password == null))
            {
                throw new SaslException("The username must not be null or empty and the password must not be null.");
            }
            NameValueCollection values = ParseDigestChallenge(Encoding.ASCII.GetString(challenge));
            string digestUri = "imap/" + values["realm"];
            string str3 = ComputeDigestResponseValue(values, this.Cnonce, digestUri, this.Username, this.Password);
            string[] strArray = new string[] { "username=" + Dquote(this.Username), "realm=" + Dquote(values["realm"]), "nonce=" + Dquote(values["nonce"]), "nc=00000001", "cnonce=" + Dquote(this.Cnonce), "digest-uri=" + Dquote(digestUri), "response=" + str3, "qop=" + values["qop"] };
            string s = string.Join(",", strArray);
            return Encoding.ASCII.GetBytes(s);
        }

        private static string ComputeDigestResponseValue(NameValueCollection challenge, string cnonce, string digestUri, string username, string password)
        {
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            string str = "00000001";
            string str2 = challenge["realm"];
            using (MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider())
            {
                byte[] bytes = encoding.GetBytes(username + ":" + str2 + ":" + password);
                bytes = provider.ComputeHash(bytes);
                string s = encoding.GetString(bytes) + ":" + challenge["nonce"] + ":" + cnonce;
                string str4 = "AUTHENTICATE:" + digestUri;
                if (!"auth".Equals(challenge["qop"]))
                {
                    str4 = str4 + ":00000000000000000000000000000000";
                }
                return MD5(MD5(s, encoding) + ":" + challenge["nonce"] + ":" + str + ":" + cnonce + ":" + challenge["qop"] + ":" + MD5(str4, encoding), encoding);
            }
        }

        protected override byte[] ComputeResponse(byte[] challenge)
        {
            if (this.Step == 1)
            {
                this.Completed = true;
            }
            byte[] buffer = (this.Step == 0) ? this.ComputeDigestResponse(challenge) : new byte[0];
            this.Step++;
            return buffer;
        }

        private static string Dquote(string s)
        {
            return ("\"" + s + "\"");
        }

        private static string GenerateCnonce()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 0x10);
        }

        private static string MD5(string s, Encoding encoding = null)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            byte[] bytes = encoding.GetBytes(s);
            byte[] buffer2 = new MD5CryptoServiceProvider().ComputeHash(bytes);
            StringBuilder builder = new StringBuilder();
            foreach (byte num in buffer2)
            {
                builder.Append(num.ToString("x2"));
            }
            return builder.ToString();
        }

        private static NameValueCollection ParseDigestChallenge(string challenge)
        {
            challenge.ThrowIfNull<string>("challenge");
            NameValueCollection values = new NameValueCollection();
            string[] strArray = challenge.Split(new char[] { ',' });
            foreach (string str in strArray)
            {
                string[] strArray2 = str.Split(new char[] { '=' }, 2);
                if (strArray2.Length == 2)
                {
                    values.Add(strArray2[0], strArray2[1].Trim(new char[] { '"' }));
                }
            }
            return values;
        }

        public override bool HasInitial
        {
            get
            {
                return false;
            }
        }

        public override bool IsCompleted
        {
            get
            {
                return this.Completed;
            }
        }

        public override string Name
        {
            get
            {
                return "DIGEST-MD5";
            }
        }

        private string Password
        {
            get
            {
                return (base.Properties.ContainsKey("Password") ? (base.Properties["Password"] as string) : null);
            }
            set
            {
                base.Properties["Password"] = value;
            }
        }

        private string Username
        {
            get
            {
                return (base.Properties.ContainsKey("Username") ? (base.Properties["Username"] as string) : null);
            }
            set
            {
                base.Properties["Username"] = value;
            }
        }
    }
}

