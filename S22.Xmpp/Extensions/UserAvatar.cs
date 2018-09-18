namespace S22.Xmpp.Extensions
{
    using S22.Xmpp;
    using S22.Xmpp.Im;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Xml;

    internal class UserAvatar : XmppExtension
    {
        private IDictionary<string, Image> cachedImages;
        private Pep pep;

        public event EventHandler<AvatarChangedEventArgs> AvatarChanged;

        public UserAvatar(XmppIm im) : base(im)
        {
            this.cachedImages = new Dictionary<string, Image>();
        }

        private string GetMimeType(Image image)
        {
            image.ThrowIfNull<Image>("image");
            foreach (ImageCodecInfo info in ImageCodecInfo.GetImageEncoders())
            {
                if (info.FormatID == image.RawFormat.Guid)
                {
                    return info.MimeType;
                }
            }
            throw new ArgumentException("The mime-type could not be determined.");
        }

        private string Hash(byte[] data)
        {
            data.ThrowIfNull<byte[]>("data");
            using (SHA1Managed managed = new SHA1Managed())
            {
                return Convert.ToBase64String(managed.ComputeHash(data));
            }
        }

        public override void Initialize()
        {
            this.pep = base.im.GetExtension<Pep>();
            this.pep.Subscribe("urn:xmpp:avatar:metadata", new Action<Jid, XmlElement>(this.onMetadata));
        }

        private void onMetadata(Jid jid, XmlElement item)
        {
            if ((item != null) && (item["metadata"] != null))
            {
                if (item["metadata"].IsEmpty)
                {
                    this.AvatarChanged.Raise<AvatarChangedEventArgs>(this, new AvatarChangedEventArgs(jid, null, null));
                }
                else
                {
                    XmlElement element = item["metadata"]["info"];
                    if (element != null)
                    {
                        string attribute = element.GetAttribute("id");
                        if (!string.IsNullOrEmpty(attribute))
                        {
                            if (!this.cachedImages.ContainsKey(attribute))
                            {
                                this.cachedImages.Add(attribute, this.RequestImage(jid, attribute));
                            }
                            Image avatar = this.cachedImages[attribute];
                            this.AvatarChanged.Raise<AvatarChangedEventArgs>(this, new AvatarChangedEventArgs(jid, attribute, avatar));
                        }
                    }
                }
            }
        }

        public void Publish(Stream stream)
        {
            stream.ThrowIfNull<Stream>("stream");
            using (Image image = Image.FromStream(stream))
            {
                string mimeType = this.GetMimeType(image);
                int width = image.Width;
                int height = image.Height;
                long length = 0L;
                string itemId = string.Empty;
                string text = string.Empty;
                using (MemoryStream stream2 = new MemoryStream())
                {
                    image.Save(stream2, image.RawFormat);
                    length = stream2.Length;
                    byte[] data = stream2.ToArray();
                    itemId = this.Hash(data);
                    text = Convert.ToBase64String(data);
                }
                this.pep.Publish("urn:xmpp:avatar:data", itemId, new XmlElement[] { Xml.Element("data", "urn:xmpp:avatar:data").Text(text) });
                this.pep.Publish("urn:xmpp:avatar:metadata", itemId, new XmlElement[] { Xml.Element("metadata", "urn:xmpp:avatar:metadata").Child(Xml.Element("info", null).Attr("bytes", length.ToString()).Attr("height", height.ToString()).Attr("width", width.ToString()).Attr("id", itemId).Attr("type", mimeType)) });
            }
        }

        public void Publish(string filePath)
        {
            using (Stream stream = File.OpenRead(filePath))
            {
                this.Publish(stream);
            }
        }

        private Image RequestImage(Jid jid, string hash)
        {
            Image image;
            jid.ThrowIfNull<Jid>("jid");
            hash.ThrowIfNull<string>("hash");
            XmlElement element = this.pep.RetrieveItem(jid, "urn:xmpp:avatar:data", hash);
            if ((element["data"] == null) || (element["data"].NamespaceURI != "urn:xmpp:avatar:data"))
            {
                throw new XmppException("Erroneous avatar data: " + element);
            }
            string innerText = element["data"].InnerText;
            try
            {
                using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(innerText)))
                {
                    image = Image.FromStream(stream);
                }
            }
            catch (Exception exception)
            {
                throw new XmppException("Invalid image data.", exception);
            }
            return image;
        }

        public override IEnumerable<string> Namespaces
        {
            get
            {
                return new string[] { "urn:xmpp:avatar:data", "urn:xmpp:avatar:metadata", "urn:xmpp:avatar:metadata+notify" };
            }
        }

        public bool Supported
        {
            get
            {
                return this.pep.Supported;
            }
        }

        public override Extension Xep
        {
            get
            {
                return Extension.UserAvatar;
            }
        }
    }
}

