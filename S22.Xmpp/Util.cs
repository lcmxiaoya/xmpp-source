namespace S22.Xmpp
{
    using S22.Xmpp.Core;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Xml;

    internal static class Util
    {
        internal static string Capitalize(this string s)
        {
            return (char.ToUpperInvariant(s[0]) + s.Substring(1));
        }

        internal static Exception ExceptionFromError(Iq errorIq, string message = null)
        {
            errorIq.ThrowIfNull<Iq>("errorIq");
            if (errorIq.Type != IqType.Error)
            {
                throw new ArgumentException("The specified Iq stanza is not of type 'error'.");
            }
            return ExceptionFromError(errorIq.Data["error"], message);
        }

        internal static Exception ExceptionFromError(XmlElement error, string message = null)
        {
            try
            {
                return new XmppErrorException(new XmppError(error), message);
            }
            catch
            {
                if (error == null)
                {
                    return new XmppException("Unspecified error.");
                }
                return new XmppException("Invalid XML error-stanza: " + error.ToXmlString(false, false));
            }
        }

        internal static T ParseEnum<T>(string value, bool ignoreCase = true) where T: struct, IComparable, IFormattable, IConvertible
        {
            value.ThrowIfNull<string>("value");
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type.");
            }
            return (T) Enum.Parse(typeof(T), value, ignoreCase);
        }

        internal static void Raise<T>(this EventHandler<T> @event, object sender, T args) where T: EventArgs
        {
            EventHandler<T> handler = @event;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        internal static void ThrowIfNull<T>(this T data) where T: class
        {
            if (data == null)
            {
                throw new ArgumentNullException();
            }
        }

        internal static void ThrowIfNull<T>(this T data, string name) where T: class
        {
            if (data == null)
            {
                throw new ArgumentNullException(name);
            }
        }

        internal static void ThrowIfNullOrEmpty(this string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException();
            }
            if (s == string.Empty)
            {
                throw new ArgumentException();
            }
        }

        internal static void ThrowIfNullOrEmpty(this string s, string name)
        {
            if (s == null)
            {
                throw new ArgumentNullException(name);
            }
            if (s == string.Empty)
            {
                throw new ArgumentException(name + " must not be empty.");
            }
        }

        internal static void ThrowIfOutOfRange(this int value, int from, int to)
        {
            if ((value < from) || (value > to))
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        internal static void ThrowIfOutOfRange(this long value, long from, long to)
        {
            if ((value < from) || (value > to))
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        internal static void ThrowIfOutOfRange(this int value, string name, int from, int to)
        {
            if ((value < from) || (value > to))
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        internal static void ThrowIfOutOfRange(this long value, string name, long from, long to)
        {
            if ((value < from) || (value > to))
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}

