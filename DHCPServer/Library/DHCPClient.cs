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
    [Serializable]
    public class DHCPClient : IEquatable<DHCPClient>
    {
        public enum TState
        {
            Released,
            Offered,
            Assigned
        }

        private byte[] _identifier = new byte[0];
        private byte[] _hardwareAddress = new byte[0];
        private string _hostName = "";
        private TState _state = TState.Released;
        private IPAddress _IPAddress = IPAddress.Any;
        private DateTime _offeredTime;
        private DateTime _leaseStartTime;
        private TimeSpan _leaseDuration;

        [XmlElement(DataType = "hexBinary")]
        public byte[] Identifier
        {
            get
            {
                return _identifier;
            }
            set
            {
                _identifier = value;
            }
        }

        [XmlElement(DataType = "hexBinary")]
        public byte[] HardwareAddress
        {
            get
            {
                return _hardwareAddress;
            }
            set
            {
                _hardwareAddress = value;
            }
        }

        public string HostName
        {
            get
            {
                return _hostName;
            }
            set
            {
                _hostName = value;
            }
        }

        public TState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        [XmlIgnore]
        internal DateTime OfferedTime
        {
            get { return _offeredTime; }
            set { _offeredTime = value; }
        }

        public DateTime LeaseStartTime
        {
            get { return _leaseStartTime; }
            set { _leaseStartTime = value; }
        }

        [XmlIgnore]
        public TimeSpan LeaseDuration
        {
            get { return _leaseDuration; }
            set { _leaseDuration = Utils.SanitizeTimeSpan(value); }
        }

        public DateTime LeaseEndTime
        {
            get
            {
                return Utils.IsInfiniteTimeSpan(_leaseDuration) ? DateTime.MaxValue : (_leaseStartTime + _leaseDuration);
            }
            set
            {
                if (value >= DateTime.MaxValue)
                {
                    _leaseDuration = Utils.InfiniteTimeSpan;
                }
                else
                {
                    _leaseDuration = value - _leaseStartTime;
                }
            }
        }

        [XmlIgnore]
        public IPAddress IPAddress
        {
            get { return _IPAddress; }
            set { _IPAddress = value; }
        }

        [XmlElement(ElementName = "IPAddress")]
        public string IPAddressAsString
        {
            get { return _IPAddress.ToString(); }
            set { _IPAddress = IPAddress.Parse(value); }
        }

        public DHCPClient()
        {
        }

        public DHCPClient Clone()
        {
            DHCPClient result=new DHCPClient();
            result._identifier = this._identifier;
            result._hardwareAddress = this._hardwareAddress;
            result._hostName = this._hostName;
            result._state = this._state;
            result._IPAddress = this._IPAddress;
            result._offeredTime = this._offeredTime;
            result._leaseStartTime = this._leaseStartTime;
            result._leaseDuration = this._leaseDuration;
            return result;
        }

        internal static DHCPClient CreateFromMessage(DHCPMessage message)
        {
            DHCPClient result = new DHCPClient();
            result._hardwareAddress = message.ClientHardwareAddress;

            DHCPOptionHostName dhcpOptionHostName = (DHCPOptionHostName)message.GetOption(TDHCPOption.HostName);

            if (dhcpOptionHostName != null)
            {
                result._hostName = dhcpOptionHostName.HostName;
            }

            DHCPOptionClientIdentifier dhcpOptionClientIdentifier = (DHCPOptionClientIdentifier)message.GetOption(TDHCPOption.ClientIdentifier);

            if (dhcpOptionClientIdentifier != null)
            {
                result._identifier = dhcpOptionClientIdentifier.Data;
            }
            else
            {
                result._identifier = message.ClientHardwareAddress;
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
                foreach (byte b in _identifier) result = (result * 31) ^ b;
                return result;
            }
        }

        public bool Equals(DHCPClient other)
        {
            return (other != null && Utils.ByteArraysAreEqual(_identifier, other._identifier));
        }

        #endregion

        public override string ToString()
        {
            return $"{Utils.BytesToHexString(this._identifier, "-")} ({this.HostName})";
        }
    }
}
