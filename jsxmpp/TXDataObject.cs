namespace jsxmpp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class TXDataObject
    {
        private Hashtable m_attributes = new Hashtable();
        private List<TXDataObject> m_subItems = new List<TXDataObject>();

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

        public string objectId { get; set; }

        public string operateType { get; set; }

        public List<TXDataObject> subItems
        {
            get
            {
                return this.m_subItems;
            }
            set
            {
                try
                {
                    if (value != null)
                    {
                        this.m_subItems = value;
                    }
                }
                catch
                {
                    this.m_subItems = new List<TXDataObject>();
                }
            }
        }
    }
}

