namespace jsxmpp
{
    using System;

    public class ServiceRequestHandler
    {
        public virtual int execute(string fromUser, ServiceRequestParam requestParam, string id, string mode)
        {
            return 1;
        }

        public virtual string getServiceId()
        {
            return "";
        }
    }
}

