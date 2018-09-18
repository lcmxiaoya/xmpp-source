namespace jsxmpp
{
    using Newtonsoft.Json;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    [Serializable]
    public class ServiceRequestParam
    {
        private string _requestType = "";
        private string _seqId = "";
        private string _serviceId;
        private string _source = "";
        private string id = "";
        private Hashtable m_attributes = new Hashtable();
        private List<TXDataObject> m_dataItems = new List<TXDataObject>();

        public string GetID()
        {
            return this.id;
        }

        public string Serialize()
        {
            if (this != null)
            {
                return JsonConvert.SerializeObject(this);
            }
            return "";
        }

        public void SetID(string strID)
        {
            this.id = strID;
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

        public List<TXDataObject> dataItems
        {
            get
            {
                return this.m_dataItems;
            }
            set
            {
                try
                {
                    if (value != null)
                    {
                        this.m_dataItems = value;
                    }
                }
                catch
                {
                }
            }
        }

        public string requestType
        {
            get
            {
                return this._requestType;
            }
            set
            {
                this._requestType = value;
            }
        }

        public string seqId
        {
            get
            {
                return this._seqId;
            }
            set
            {
                this._seqId = value;
            }
        }

        public string serviceId
        {
            get
            {
                return this._serviceId;
            }
            set
            {
                this._serviceId = value;
            }
        }

        public string source
        {
            get
            {
                return this._source;
            }
            set
            {
                this._source = value;
            }
        }
    }
}

