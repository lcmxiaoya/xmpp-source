namespace jsxmpp
{
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;

    public class FileDescription
    {
        private string _uid = "";
        private Hashtable m_attributes = new Hashtable();

        public string GetFileName()
        {
            return this.fileName;
        }

        public Hashtable attributes
        {
            get
            {
                return this.m_attributes;
            }
            set
            {
                try
                {
                    if (value != null)
                    {
                        this.m_attributes = value;
                    }
                }
                catch
                {
                    this.m_attributes = new Hashtable();
                }
            }
        }

        public int fileCategory { get; set; }

        public string fileName { get; set; }

        public int fileSize { get; set; }

        public int fileType { get; set; }

        public string uid
        {
            get
            {
                if (!string.IsNullOrEmpty(this._uid))
                {
                    return this._uid;
                }
                return Guid.NewGuid().ToString().Replace("-", "");
            }
            set
            {
                this._uid = value;
            }
        }
    }
}

