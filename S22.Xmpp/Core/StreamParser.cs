namespace S22.Xmpp.Core
{
    using S22.Xmpp;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Xml;

    internal class StreamParser : IDisposable
    {
        private bool leaveOpen;
        private XmlReader reader;
        private Stream stream;

        public StreamParser(Stream stream, bool leaveOpen = false)
        {
            stream.ThrowIfNull<Stream>("stream");
            this.leaveOpen = leaveOpen;
            this.stream = stream;
            XmlReaderSettings settings = new XmlReaderSettings {
                IgnoreProcessingInstructions = true,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            this.reader = XmlReader.Create(stream, settings);
            this.ReadRootElement();
        }

        public void Close()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            this.reader.Close();
            if (!this.leaveOpen)
            {
                this.stream.Close();
            }
        }

        public XmlElement NextElement(params string[] expected)
        {
            this.reader.Read();
            if ((this.reader.NodeType == XmlNodeType.EndElement) && (this.reader.Name == "stream:stream"))
            {
                throw new IOException("The server has closed the XML stream.");
            }
            if (this.reader.NodeType != XmlNodeType.Element)
            {
                throw new XmlException(string.Concat(new object[] { "Unexpected node: '", this.reader.Name, "' of type ", this.reader.NodeType }));
            }
            if (!this.reader.IsStartElement())
            {
                throw new XmlException("Not a start element: " + this.reader.Name);
            }
            using (XmlReader reader = this.reader.ReadSubtree())
            {
                reader.Read();
                string s = reader.ReadOuterXml();
                XmlDocument document = new XmlDocument();
                using (StringReader reader2 = new StringReader(s))
                {
                    using (XmlTextReader reader3 = new XmlTextReader(reader2))
                    {
                        document.Load(reader3);
                    }
                }
                XmlElement firstChild = (XmlElement) document.FirstChild;
                if (firstChild.Name == "stream:error")
                {
                    string str2 = (firstChild.FirstChild != null) ? firstChild.FirstChild.Name : "undefined";
                    throw new IOException("Unrecoverable stream error: " + str2);
                }
                if (!((expected.Length <= 0) || expected.Contains<string>(firstChild.Name)))
                {
                    throw new XmlException("Unexpected XML element: " + firstChild.Name);
                }
                return firstChild;
            }
        }

        private void ReadRootElement()
        {
            while (this.reader.Read())
            {
                XmlNodeType nodeType = this.reader.NodeType;
                if (nodeType != XmlNodeType.Element)
                {
                    if (nodeType != XmlNodeType.XmlDeclaration)
                    {
                        throw new XmlException("Unexpected node: " + this.reader.Name);
                    }
                }
                else
                {
                    if (this.reader.Name != "stream:stream")
                    {
                        throw new XmlException("Unexpected document root: " + this.reader.Name);
                    }
                    string attribute = this.reader.GetAttribute("xml:lang");
                    if (!string.IsNullOrEmpty(attribute))
                    {
                        this.Language = new CultureInfo(attribute);
                    }
                    break;
                }
            }
        }

        public CultureInfo Language { get; private set; }
    }
}

