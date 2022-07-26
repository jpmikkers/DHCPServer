using System;
using System.Net;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;

namespace DHCPServerApp
{
    [Serializable()]
    public class XmlSerializableIPAddress : IXmlSerializable
    {
        public IPAddress Address { get; set; }

        public XmlSerializableIPAddress()
        {
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            // https://www.codeproject.com/Articles/43237/How-to-Implement-IXmlSerializable-Correctly
            reader.MoveToContent();
            var isEmptyElement = reader.IsEmptyElement;
            reader.ReadStartElement();
            if (!isEmptyElement)
            {
                Address = IPAddress.Parse(reader.ReadContentAsString());
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            if(Address!=null) writer.WriteString(Address.ToString());
        }
    }
}