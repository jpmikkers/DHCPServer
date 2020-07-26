/*

Copyright (c) 2020 Jean-Paul Mikkers

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
using System.Net;
using System.Collections.Generic;

namespace DHCPServerApp
{
    [Serializable()]
    public class DHCPServerConfiguration
    {
        private string m_Name;
        private IPAddress m_Address;
        private IPAddress m_NetMask;
        private IPAddress m_PoolStart;
        private IPAddress m_PoolEnd;
        private int m_LeaseTime;
        private int m_OfferTime;
        private int m_MinimumPacketSize;
        private List<OptionConfiguration> m_Options;
        private List<ReservationConfiguration> m_Reservations;

        public string Name
        {
            get { return m_Name; } 
            set { m_Name = value; }
        }

        public string Address
        {
            get
            {
                return m_Address.ToString();
            }
            set
            {
                m_Address = IPAddress.Parse(value);
            }
        }

        public string NetMask
        {
            get
            {
                return m_NetMask.ToString();
            }
            set
            {
                m_NetMask = IPAddress.Parse(value);
            }
        }

        public string PoolStart
        {
            get
            {
                return m_PoolStart.ToString();
            }
            set
            {
                m_PoolStart = IPAddress.Parse(value);
            }
        }

        public string PoolEnd
        {
            get
            {
                return m_PoolEnd.ToString();
            }
            set
            {
                m_PoolEnd = IPAddress.Parse(value);
            }
        }

        public int LeaseTime
        {
            get
            {
                return m_LeaseTime;
            }
            set
            {
                m_LeaseTime = value;
            }
        }

        public int OfferTime
        {
            get
            {
                return m_OfferTime;
            }
            set
            {
                m_OfferTime = value;
            }
        }

        public int MinimumPacketSize
        {
            get
            {
                return m_MinimumPacketSize;
            }
            set
            {
                m_MinimumPacketSize = value;
            }
        }

        public List<OptionConfiguration> Options
        {
            get
            {
                return m_Options;
            }
            set
            {
                m_Options = value;
            }
        }

        public List<ReservationConfiguration> Reservations
        {
            get
            {
                return m_Reservations;
            }
            set
            {
                m_Reservations = value;
            }
        }

        public DHCPServerConfiguration()
        {
            Name = "DHCP";
            Address = IPAddress.Loopback.ToString();
            NetMask = "255.255.255.0";
            PoolStart = "0.0.0.0";
            PoolEnd = "255.255.255.255";
            LeaseTime = (int)TimeSpan.FromDays(1.0).TotalSeconds;
            OfferTime = 30;
            MinimumPacketSize = 576;
            Options = new List<OptionConfiguration>();
            Reservations = new List<ReservationConfiguration>();
        }

        public DHCPServerConfiguration Clone()
        {
            return DeepCopier.Copy(this);
        }
    }
}