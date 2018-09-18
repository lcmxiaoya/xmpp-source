namespace S22.Xmpp
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Net;

    public class HttpHelper
    {
        public static long GetHttpLength(Stream stream, string fileName)
        {
            try
            {
                fileName = fileName.ToLower();
                Bitmap bitmap = new Bitmap(stream);
                MemoryStream stream2 = new MemoryStream();
                bitmap.Save(stream2, GetImageFormat(fileName));
                return stream2.Length;
            }
            catch
            {
                return 0L;
            }
        }

        public static ImageFormat GetImageFormat(string fileName)
        {
            if (fileName.Contains(".jpg"))
            {
                return ImageFormat.Jpeg;
            }
            if (fileName.Contains(".bmp"))
            {
                return ImageFormat.Bmp;
            }
            if (fileName.Contains(".emf"))
            {
                return ImageFormat.Emf;
            }
            if (fileName.Contains(".exif"))
            {
                return ImageFormat.Exif;
            }
            if (fileName.Contains(".gif"))
            {
                return ImageFormat.Gif;
            }
            if (fileName.Contains(".icon"))
            {
                return ImageFormat.Icon;
            }
            if (fileName.Contains(".png"))
            {
                return ImageFormat.Png;
            }
            if (fileName.Contains(".tiff"))
            {
                return ImageFormat.Tiff;
            }
            if (fileName.Contains(".wmf"))
            {
                return ImageFormat.Wmf;
            }
            return null;
        }

        public static bool HttpDown(string url, string filePath, string oldurl)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                request.Referer = oldurl;
                request.UserAgent = " Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/33.0.1750.154 Safari/537.36";
                request.ContentType = "application/octet-stream";
                Stream responseStream = (request.GetResponse() as HttpWebResponse).GetResponseStream();
                FileStream stream2 = System.IO.File.Create(filePath);
                int count = 0;
                do
                {
                    byte[] buffer = new byte[0x400];
                    count = responseStream.Read(buffer, 0, 0x400);
                    stream2.Write(buffer, 0, count);
                }
                while (count > 0);
                stream2.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

