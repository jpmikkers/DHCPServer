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
using System.Net;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.IO;
using System.ComponentModel;
using GitHub.JPMikkers.DHCP;

namespace DHCPServerApp
{
    [Serializable()]
    public class ReservationConfiguration
    {
        private IPAddress m_PoolStart;
        private IPAddress m_PoolEnd;
        private bool m_Preempt;

        public string MacTaste
        {
            get;
            set;
        }

        public string HostName
        {
            get;
            set;
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

        [DefaultValue(false)]
        public bool Preemt
        {
            get
            {
                return m_Preempt;
            }
            set
            {
                m_Preempt = value;
            }
        }

        public ReservationItem ConstructReservationItem()
        {
            return new ReservationItem()
            { 
                HostName = this.HostName, 
                MacTaste = this.MacTaste, 
                PoolStart = m_PoolStart, 
                PoolEnd = m_PoolEnd,
                Preempt = m_Preempt,
            };
        }
    }

    [Serializable()]
    [XmlInclude(typeof(OptionConfigurationTFTPServerName))]
    [XmlInclude(typeof(OptionConfigurationBootFileName))]
    [XmlInclude(typeof(OptionConfigurationVendorSpecificInformation))]
    [XmlInclude(typeof(OptionConfigurationGeneric))]
    [XmlInclude(typeof(OptionConfigurationVendorClassIdentifier))]
    public abstract class OptionConfiguration
    {
        public OptionMode Mode;

        [OptionalField]
        public bool ZeroTerminatedStrings;

        public OptionConfiguration()
        {
            Mode = OptionMode.Default;
        }

        public OptionItem ConstructOptionItem()
        {
            return new OptionItem(Mode,ConstructDHCPOption());
        }

        public abstract IDHCPOption ConstructDHCPOption();

        protected IDHCPOption FixZString(IDHCPOption i)
        {
            i.ZeroTerminatedStrings = ZeroTerminatedStrings;
            return i;
        }
    }

    [Serializable()]
    public class OptionConfigurationTFTPServerName : OptionConfiguration
    {
        public string Name;

        public OptionConfigurationTFTPServerName()
        {
            Name = "";
        }

        public override IDHCPOption ConstructDHCPOption()
        {
            return FixZString(new DHCPOptionTFTPServerName(Name));
        }
    }

    [Serializable()]
    public class OptionConfigurationBootFileName : OptionConfiguration
    {
        public string Name;

        public OptionConfigurationBootFileName()
        {
            Name = "";
        }

        public override IDHCPOption ConstructDHCPOption()
        {
            return FixZString(new DHCPOptionBootFileName(Name));
        }
    }

    [Serializable()]
    public class OptionConfigurationVendorSpecificInformation : OptionConfiguration
    {
        public string Information;

        public OptionConfigurationVendorSpecificInformation()
        {
            Information = "";
        }

        public override IDHCPOption ConstructDHCPOption()
        {
            return FixZString(new DHCPOptionVendorSpecificInformation(Information));
        }
    }

    [Serializable()]
    public class OptionConfigurationVendorClassIdentifier : OptionConfiguration
    {
        public string DataAsString;
        public string DataAsHex;

        public OptionConfigurationVendorClassIdentifier()
        {
            DataAsString = "";
            DataAsHex = "";
        }

        public override IDHCPOption ConstructDHCPOption()
        {
            byte[] data;

            if(string.IsNullOrEmpty(DataAsString))
            {
                data = Utils.HexStringToBytes(DataAsHex);
            }
            else
            {      
                MemoryStream m = new MemoryStream();
                ParseHelper.WriteString(m,DataAsString);
                m.Flush();
                data = m.ToArray();
            }

            return FixZString(new DHCPOptionVendorClassIdentifier(data));
        }
    }

    [Serializable()]
    public class OptionConfigurationGeneric : OptionConfiguration
    {
        public int Option;
        public string Data;

        public OptionConfigurationGeneric()
        {
        }

        public override IDHCPOption ConstructDHCPOption()
        {
            return new DHCPOptionGeneric((TDHCPOption) Option, Utils.HexStringToBytes(Data));
        }
    }

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

    [Serializable()]
    public class DHCPServerConfigurationList : BindingList<DHCPServerConfiguration>
    {
        private static XmlSerializer serializer = new XmlSerializer(typeof(DHCPServerConfigurationList));

        public static DHCPServerConfigurationList Read(string file)
        {
            DHCPServerConfigurationList result;

            if (File.Exists(file))
            {
                using (Stream s = File.OpenRead(file))
                {
                    result = (DHCPServerConfigurationList)serializer.Deserialize(s);
                }
            }
            else
            {
                result = new DHCPServerConfigurationList();
            }

            return result;
        }

        public void Write(string file)
        {
            string dirName = Path.GetDirectoryName(file);

            if(!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            using(Stream s = File.Open(file,FileMode.Create))
            {
                serializer.Serialize(s, this);
            }
        }
    }
}