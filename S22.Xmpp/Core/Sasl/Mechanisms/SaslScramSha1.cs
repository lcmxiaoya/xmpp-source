namespace S22.Xmpp.Core.Sasl.Mechanisms
{
    using S22.Xmpp;
    using S22.Xmpp.Core.Sasl;
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    internal class SaslScramSha1 : SaslMechanism
    {
        private string AuthMessage;
        private string Cnonce;
        private bool Completed;
        private byte[] SaltedPassword;
        private int Step;

        private SaslScramSha1()
        {
            this.Completed = false;
            this.Cnonce = GenerateCnonce();
            this.Step = 0;
        }

        public SaslScramSha1(string username, string password)
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

        internal SaslScramSha1(string username, string password, string cnonce) : this(username, password)
        {
            this.Cnonce = cnonce;
        }

        private byte[] ComputeFinalResponse(byte[] challenge)
        {
            NameValueCollection values = this.ParseServerFirstMessage(challenge);
            string salt = values["s"];
            string nonce = values["r"];
            int count = int.Parse(values["i"]);
            if (!this.VerifyServerNonce(nonce))
            {
                throw new SaslException("Invalid server nonce: " + nonce);
            }
            string str3 = "n=" + SaslPrep(this.Username) + ",r=" + this.Cnonce;
            string str4 = Encoding.UTF8.GetString(challenge);
            string str5 = "c=" + Convert.ToBase64String(Encoding.UTF8.GetBytes("n,,")) + ",r=" + nonce;
            this.AuthMessage = str3 + "," + str4 + "," + str5;
            this.SaltedPassword = this.Hi(this.Password, salt, count);
            byte[] data = this.HMAC(this.SaltedPassword, "Client Key");
            byte[] key = this.H(data);
            byte[] b = this.HMAC(key, this.AuthMessage);
            byte[] inArray = this.Xor(data, b);
            return Encoding.UTF8.GetBytes(str5 + ",p=" + Convert.ToBase64String(inArray));
        }

        private byte[] ComputeInitialResponse()
        {
            return Encoding.UTF8.GetBytes("n,,n=" + SaslPrep(this.Username) + ",r=" + this.Cnonce);
        }

        protected override byte[] ComputeResponse(byte[] challenge)
        {
            if (string.IsNullOrEmpty(this.Username) || (this.Password == null))
            {
                throw new SaslException("The username must not be null or empty and the password must not be null.");
            }
            if (this.Step == 2)
            {
                this.Completed = true;
            }
            byte[] buffer = (this.Step == 0) ? this.ComputeInitialResponse() : ((this.Step == 1) ? this.ComputeFinalResponse(challenge) : this.VerifyServerSignature(challenge));
            this.Step++;
            return buffer;
        }

        private static string GenerateCnonce()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 0x10);
        }

        private byte[] H(byte[] data)
        {
            using (SHA1Managed managed = new SHA1Managed())
            {
                return managed.ComputeHash(data);
            }
        }

        private byte[] Hi(string password, string salt, int count)
        {
            byte[] buffer = Convert.FromBase64String(salt);
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, buffer, count))
            {
                return bytes.GetBytes(20);
            }
        }

        private byte[] HMAC(byte[] key, byte[] data)
        {
            using (HMACSHA1 hmacsha = new HMACSHA1(key))
            {
                return hmacsha.ComputeHash(data);
            }
        }

        private byte[] HMAC(byte[] key, string data)
        {
            return this.HMAC(key, Encoding.UTF8.GetBytes(data));
        }

        private NameValueCollection ParseServerFirstMessage(byte[] challenge)
        {
            challenge.ThrowIfNull<byte[]>("challenge");
            string str = Encoding.UTF8.GetString(challenge);
            NameValueCollection values = new NameValueCollection();
            foreach (string str2 in str.Split(new char[] { ',' }))
            {
                int index = str2.IndexOf('=');
                if (index >= 0)
                {
                    string name = str2.Substring(0, index);
                    string str4 = str2.Substring(index + 1);
                    values.Add(name, str4);
                }
            }
            return values;
        }

        private static string SaslPrep(string s)
        {
            return s.Replace("=", "=3D").Replace(",", "=2C");
        }

        private bool VerifyServerNonce(string nonce)
        {
            return nonce.StartsWith(this.Cnonce);
        }

        private byte[] VerifyServerSignature(byte[] challenge)
        {
            string str = Encoding.UTF8.GetString(challenge);
            if (!str.StartsWith("v="))
            {
                return Encoding.UTF8.GetBytes("*");
            }
            byte[] first = Convert.FromBase64String(str.Substring(2));
            byte[] key = this.HMAC(this.SaltedPassword, "Server Key");
            byte[] second = this.HMAC(key, this.AuthMessage);
            return (first.SequenceEqual<byte>(second) ? new byte[0] : Encoding.UTF8.GetBytes("*"));
        }

        private byte[] Xor(byte[] a, byte[] b)
        {
            a.ThrowIfNull<byte[]>("a");
            b.ThrowIfNull<byte[]>("b");
            if (a.Length != b.Length)
            {
                throw new ArgumentException();
            }
            byte[] buffer = new byte[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                buffer[i] = (byte) (a[i] ^ b[i]);
            }
            return buffer;
        }

        public override bool HasInitial
        {
            get
            {
                return true;
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
                return "SCRAM-SHA-1";
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

