namespace S22.Xmpp.Core.Sasl.Mechanisms
{
    using S22.Xmpp;
    using S22.Xmpp.Core.Sasl;
    using System;
    using System.Text;

    internal class SaslPlain : SaslMechanism
    {
        private bool Completed;

        private SaslPlain()
        {
            this.Completed = false;
        }

        public SaslPlain(string username, string password)
        {
            this.Completed = false;
            username.ThrowIfNull<string>("username");
            if (username == string.Empty)
            {
                throw new ArgumentException("The username must not be empty.");
            }
            password.ThrowIfNull<string>("password");
            this.Username = username;
            this.Password = password;
        }

        protected override byte[] ComputeResponse(byte[] challenge)
        {
            if (string.IsNullOrEmpty(this.Username) || (this.Password == null))
            {
                throw new SaslException("The username must not be null or empty and the password must not be null.");
            }
            this.Completed = true;
            return Encoding.UTF8.GetBytes("\0" + this.Username + "\0" + this.Password);
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
                return "PLAIN";
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

