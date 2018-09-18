namespace S22.Xmpp.Core.Sasl
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    internal abstract class SaslMechanism
    {
        internal SaslMechanism()
        {
            this.Properties = new Dictionary<string, object>();
        }

        protected abstract byte[] ComputeResponse(byte[] challenge);
        public string GetResponse(string challenge)
        {
            string str;
            try
            {
                byte[] buffer = string.IsNullOrEmpty(challenge) ? new byte[0] : Convert.FromBase64String(challenge);
                str = Convert.ToBase64String(this.ComputeResponse(buffer));
            }
            catch (Exception exception)
            {
                throw new SaslException("The challenge-response could not be retrieved.", exception);
            }
            return str;
        }

        public byte[] GetResponse(byte[] challenge)
        {
            return this.ComputeResponse(challenge);
        }

        public abstract bool HasInitial { get; }

        public abstract bool IsCompleted { get; }

        public abstract string Name { get; }

        public Dictionary<string, object> Properties { get; private set; }
    }
}

