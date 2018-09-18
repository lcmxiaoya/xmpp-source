namespace S22.Xmpp.Extensions.Dataforms
{
    using S22.Xmpp;
    using System;
    using System.Xml;

    internal static class DataFormFactory
    {
        public static DataForm Create(XmlElement element)
        {
            DataForm form;
            element.ThrowIfNull<XmlElement>("element");
            if ((element.Name != "x") || (element.NamespaceURI != "jabber:x:data"))
            {
                throw new ArgumentException("Invalid root element: " + element.Name);
            }
            string attribute = element.GetAttribute("type");
            if (string.IsNullOrEmpty(attribute))
            {
                throw new ArgumentException("Missing 'type' attribute.");
            }
            try
            {
                DataFormType type = Util.ParseEnum<DataFormType>(attribute, true);
                switch (type)
                {
                    case DataFormType.Form:
                        return new RequestForm(element);

                    case DataFormType.Submit:
                        return new SubmitForm(element);

                    case DataFormType.Cancel:
                        return new CancelForm(element);

                    case DataFormType.Result:
                        return new ResultForm(element);
                }
                throw new ArgumentException("Invalid form type: " + type);
            }
            catch (Exception exception)
            {
                throw new XmlException("Invalid data-form element.", exception);
            }
            return form;
        }
    }
}

