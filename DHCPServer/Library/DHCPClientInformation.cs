/*

Copyright (c) 2010 Jean-Paul Mikkers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/
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
        private List<DHCPClient> m_Clients = new List<DHCPClient>();

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
                return m_Clients;
            }
            set
            {
                m_Clients = value;
            }
        }

        private static XmlSerializer serializer = new XmlSerializer(typeof(DHCPClientInformation));

        public static DHCPClientInformation Read(string file)
        {
            DHCPClientInformation result;

            if (File.Exists(file))
            {
                using (Stream s = File.OpenRead(file))
                {
                    result = (DHCPClientInformation)serializer.Deserialize(s);
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
                serializer.Serialize(s, this);
                s.Flush();
            }
        }
    }
}
