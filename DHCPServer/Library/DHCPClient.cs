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
using System.Net.Configuration;
using System.Xml.Serialization;

namespace GitHub.JPMikkers.DHCP
{
    [Serializable]
    public class DHCPClient : IEquatable<DHCPClient>
    {
        public enum TState
        {
            Released,
            Offered,
            Assigned
        }

        private byte[] m_Identifier = new byte[0];
        private byte[] m_HardwareAddress = new byte[0];
        private string m_HostName = "";
        private TState m_State = TState.Released;
        private IPAddress m_IPAddress = IPAddress.Any;
        private DateTime m_OfferedTime;
        private DateTime m_LeaseStartTime;
        private TimeSpan m_LeaseDuration;

        [XmlElement(DataType = "hexBinary")]
        public byte[] Identifier
        {
            get
            {
                return m_Identifier;
            }
            set
            {
                m_Identifier = value;
            }
        }

        [XmlElement(DataType = "hexBinary")]
        public byte[] HardwareAddress
        {
            get
            {
                return m_HardwareAddress;
            }
            set
            {
                m_HardwareAddress = value;
            }
        }

        public string HostName
        {
            get
            {
                return m_HostName;
            }
            set
            {
                m_HostName = value;
            }
        }

        public TState State
        {
            get
            {
                return m_State;
            }
            set
            {
                m_State = value;
            }
        }

        [XmlIgnore]
        internal DateTime OfferedTime
        {
            get { return m_OfferedTime; }
            set { m_OfferedTime = value; }
        }

        public DateTime LeaseStartTime
        {
            get { return m_LeaseStartTime; }
            set { m_LeaseStartTime = value; }
        }

        [XmlIgnore]
        public TimeSpan LeaseDuration
        {
            get { return m_LeaseDuration; }
            set { m_LeaseDuration = Utils.SanitizeTimeSpan(value); }
        }

        public DateTime LeaseEndTime
        {
            get
            {
                return Utils.IsInfiniteTimeSpan(m_LeaseDuration) ? DateTime.MaxValue : (m_LeaseStartTime + m_LeaseDuration);
            }
            set
            {
                if (value >= DateTime.MaxValue)
                {
                    m_LeaseDuration = Utils.InfiniteTimeSpan;
                }
                else
                {
                    m_LeaseDuration = value - m_LeaseStartTime;
                }
            }
        }

        [XmlIgnore]
        public IPAddress IPAddress
        {
            get { return m_IPAddress; }
            set { m_IPAddress = value; }
        }

        [XmlElement(ElementName = "IPAddress")]
        public string IPAddressAsString
        {
            get { return m_IPAddress.ToString(); }
            set { m_IPAddress = IPAddress.Parse(value); }
        }

        public DHCPClient()
        {
        }

        public DHCPClient Clone()
        {
            DHCPClient result=new DHCPClient();
            result.m_Identifier = this.m_Identifier;
            result.m_HardwareAddress = this.m_HardwareAddress;
            result.m_HostName = this.m_HostName;
            result.m_State = this.m_State;
            result.m_IPAddress = this.m_IPAddress;
            result.m_OfferedTime = this.m_OfferedTime;
            result.m_LeaseStartTime = this.m_LeaseStartTime;
            result.m_LeaseDuration = this.m_LeaseDuration;
            return result;
        }

        internal static DHCPClient CreateFromMessage(DHCPMessage message)
        {
            DHCPClient result = new DHCPClient();
            result.m_HardwareAddress = message.ClientHardwareAddress;

            DHCPOptionHostName dhcpOptionHostName = (DHCPOptionHostName)message.GetOption(TDHCPOption.HostName);

            if (dhcpOptionHostName != null)
            {
                result.m_HostName = dhcpOptionHostName.HostName;
            }

            DHCPOptionClientIdentifier dhcpOptionClientIdentifier = (DHCPOptionClientIdentifier)message.GetOption(TDHCPOption.ClientIdentifier);

            if (dhcpOptionClientIdentifier != null)
            {
                result.m_Identifier = dhcpOptionClientIdentifier.Data;
            }
            else
            {
                result.m_Identifier = message.ClientHardwareAddress;
            }

            return result;
        }

        #region IEquatable and related

        public override bool Equals(object obj)
        {
            return Equals(obj as DHCPClient);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = 0;
                foreach (byte b in m_Identifier) result = (result * 31) ^ b;
                return result;
            }
        }

        public bool Equals(DHCPClient other)
        {
            return (other != null && Utils.ByteArraysAreEqual(m_Identifier, other.m_Identifier));
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0} ({1})", Utils.BytesToHexString(this.m_Identifier, "-"), this.HostName);
        }
    }
}
