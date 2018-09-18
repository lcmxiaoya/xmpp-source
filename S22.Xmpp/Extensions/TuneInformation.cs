namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [Serializable]
    public class TuneInformation
    {
        public TuneInformation(string title = null, string artist = null, string track = null, int length = 0, int rating = 0, string source = null, string uri = null)
        {
            length.ThrowIfOutOfRange(0, 0x7fff);
            rating.ThrowIfOutOfRange(0, 10);
            this.Title = title;
            this.Artist = artist;
            this.Track = track;
            this.Length = length;
            this.Rating = rating;
            this.Source = source;
            this.Uri = uri;
        }

        public string Artist { get; private set; }

        public int Length { get; private set; }

        public int Rating { get; private set; }

        public string Source { get; private set; }

        public string Title { get; private set; }

        public string Track { get; private set; }

        public string Uri { get; private set; }
    }
}

