namespace S22.Xmpp
{
    using System;
    using System.Xml;

    public class XmlHelper
    {
        public static string FilterChar(string strXml)
        {
            strXml = strXml.Replace("&", "&amp;");
            strXml = strXml.Replace("<", "&lt;");
            strXml = strXml.Replace(">", "&gt;");
            strXml = strXml.Replace("\"", "&quot;");
            strXml = strXml.Replace("'", "&apos;");
            return strXml;
        }

        public static XmlNode findNode(XmlNodeList list, string strNodeName)
        {
            foreach (XmlNode node in list)
            {
                if (node.Name.Equals(strNodeName))
                {
                    return node;
                }
                if (node.ChildNodes.Count > 0)
                {
                    XmlNode node2 = findNode(node.ChildNodes, strNodeName);
                    if (node2 != null)
                    {
                        return node2;
                    }
                }
            }
            return null;
        }

        public static string ResumeChar(string strXml)
        {
            strXml = strXml.Replace("&amp;", "&");
            strXml = strXml.Replace("&lt;", "<");
            strXml = strXml.Replace("&gt;", ">");
            strXml = strXml.Replace("&quot;", "\"");
            strXml = strXml.Replace("&apos;", "'");
            return strXml;
        }
    }
}

