using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Xml.Serialization;

namespace GitHub.JPMikkers.DHCP
{
    [Serializable()]
    public class DHCPClientInformation
    {
        private List<DHCPClient> _clients = new List<DHCPClient>();

        public DateTime TimeStamp
        {
            get
            {
                return DateTime.Now;
            }
            set
            {
            }
        }

        public List<DHCPClient> Clients
        {
            get
            {
                return _clients;
            }
            set
            {
                _clients = value;
            }
        }

        private static XmlSerializer s_serializer = new XmlSerializer(typeof(DHCPClientInformation));

        public static DHCPClientInformation Read(string file)
        {
            DHCPClientInformation result;

            if (File.Exists(file))
            {
                using (Stream s = File.OpenRead(file))
                {
                    result = (DHCPClientInformation)s_serializer.Deserialize(s);
                }
            }
            else
            {
                result = new DHCPClientInformation();
            }

            return result;
        }

        public void Write(string file)
        {
            string dirName = Path.GetDirectoryName(file);

            if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            using (Stream s = File.Open(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                s_serializer.Serialize(s, this);
                s.Flush();
            }
        }
    }
}
