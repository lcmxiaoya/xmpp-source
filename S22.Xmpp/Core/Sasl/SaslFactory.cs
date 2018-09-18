namespace S22.Xmpp.Core.Sasl
{
    using S22.Xmpp;
    using S22.Xmpp.Core.Sasl.Mechanisms;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    internal static class SaslFactory
    {
        static SaslFactory()
        {
            Mechanisms = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
            Dictionary<string, Type> dictionary2 = new Dictionary<string, Type>();
            dictionary2.Add("PLAIN", typeof(SaslPlain));
            dictionary2.Add("DIGEST-MD5", typeof(SaslDigestMd5));
            dictionary2.Add("SCRAM-SHA-1", typeof(SaslScramSha1));
            Dictionary<string, Type> dictionary = dictionary2;
            foreach (string str in dictionary.Keys)
            {
                Mechanisms.Add(str, dictionary[str]);
            }
        }

        public static void Add(string name, Type t)
        {
            name.ThrowIfNull<string>("name");
            t.ThrowIfNull<Type>("t");
            if (!t.IsSubclassOf(typeof(SaslMechanism)))
            {
                throw new ArgumentException("The type t must be a subclass of Sasl.SaslMechanism");
            }
            try
            {
                Mechanisms.Add(name, t);
            }
            catch (Exception exception)
            {
                throw new SaslException("Registration of Sasl mechanism failed.", exception);
            }
        }

        public static SaslMechanism Create(string name)
        {
            name.ThrowIfNull<string>("name");
            if (!Mechanisms.ContainsKey(name))
            {
                throw new SaslException("A Sasl mechanism with the specified name is not registered with Sasl.SaslFactory.");
            }
            Type type = Mechanisms[name];
            return (Activator.CreateInstance(type, true) as SaslMechanism);
        }

        private static Dictionary<string, Type> Mechanisms
        {
            get; set;
        }
    }
}

