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
using System.IO;
using System.Net;
using System.Text;

namespace CodePlex.JPMikkers.DHCP
{
    public enum TDHCPOption
    {
        // 3: RFC 1497 Vendor Extensions
        Pad = 0,
        SubnetMask = 1,
        TimeOffset = 2,
        Router = 3,
        TimeServer = 4,
        NameServer = 5,
        DomainNameServer = 6,
        LogServer = 7,
        CookieServer = 8,
        LPRServer = 9,
        ImpressServer = 10,
        ResourceLocationServer = 11,
        HostName = 12,
        BootFileSize = 13,
        MeritDumpFile = 14,
        DomainName = 15,
        SwapServer = 16,
        RootPath = 17,
        ExtensionPath = 18,

        // 4: IP Layer Parameters per Host
        IPForwardingEnable = 19,
        NonLocalSourceRoutingEnable = 20,
        PolicyFilter = 21,
        MaximumDatagramReassembly = 22,
        DefaultIPTTL = 23,
        PathMTUAgingTimeout = 24,
        PathMTUPlateauTable = 25,

        // 5: IP Layer Parameters per Interface
        InterfaceMTU = 26,
        AllSubnetsAreLocal = 27,
        BroadcastAddress = 28,
        PerformMaskDiscovery = 29,
        MaskSupplier = 30,
        PerformRouterDiscovery = 31,
        RouterSolicitationAddress = 32,
        StaticRoute = 33,

        // 6: Link Layer Parameters per Interface
        TrailerEncapsulation = 34,
        ARPCacheTimeout = 35,
        EthernetEncapsulation = 36,

        // 7: TCP Parameters
        TCPDefaultTTL = 37,
        TCPKeepaliveInterval = 38,
        TCPKeepaliveGarbage = 39,

        // 8: Application and Service parameters
        NetworkInformationServiceDomain = 40,
        NetworkInformationServiceServers = 41,
        NetworkTimeProtocolServers = 42,
        VendorSpecificInformation = 43,
        NetBIOSOverTCPIPNameServer = 44,
        NetBIOSOverTCPIPDatagramDistributionServer = 45,
        NetBIOSOverTCPIPNodeType = 46,
        NetBIOSOverTCPIPScope = 47,
        XWindowSystemFontServer = 48,
        XWindowSystemDisplayManager = 49,
        NetworkInformationServicePlusDomain = 64,
        NetworkInformationServicePlusServers = 65,
        MobileIPHomeAgent = 68,
        SimpleMailTransportProtocolServer = 69,
        PostOfficeProtocolServer = 70,
        NetworkNewsTransportProtocolServer = 71,
        DefaultWorldWideWebServer = 72,
        DefaultFingerServer = 73,
        DefaultInternetRelayChat = 74,
        StreetTalkServer = 75,
        StreetTalkDirectoryAssistanceServer = 76,

        // 9: DHCP Extensions
        RequestedIPAddress = 50,    // this option is used in a client request to allow the client to request a particular IP address to be assigned
        IPAddressLeaseTime = 51,
        OptionOverload = 52,
        MessageType = 53,
        ServerIdentifier = 54,
        ParameterRequestList = 55,
        Message = 56,
        MaximumDHCPMessageSize = 57,
        RenewalTimeValue = 58,
        RebindingTimeValue = 59,
        VendorClassIdentifier = 60,
        ClientIdentifier = 61,
        TFTPServerName = 66,
        BootFileName = 67,

        FullyQualifiedDomainName = 81,              // RFC4702

        ClientSystemArchitectureType = 93,          // RFC4578
        ClientNetworkInterfaceIdentifier = 94,      // RFC4578
        ClientMachineIdentifier = 97,               // RFC4578

        AutoConfigure = 116,                        // RFC2563
        ClasslessStaticRoutesA = 121,               // RFC3442

        /*
            128   TFPT Server IP address                        // RFC 4578 
            129   Call Server IP address                        // RFC 4578 
            130   Discrimination string                         // RFC 4578 
            131   Remote statistics server IP address           // RFC 4578 
            132   802.1P VLAN ID
            133   802.1Q L2 Priority
            134   Diffserv Code Point
            135   HTTP Proxy for phone-specific applications    
         */ 

        ClasslessStaticRoutesB = 249,

        End = 255,
    }

    public enum TDHCPMessageType
    {
        DISCOVER = 1,
        OFFER,
        REQUEST,
        DECLINE,
        ACK,
        NAK,
        RELEASE,
        INFORM,
        Undefined
    }

    public interface IDHCPOption
    {
        bool ZeroTerminatedStrings { get; set; }
        TDHCPOption OptionType { get; }
        IDHCPOption FromStream(Stream s);
        void ToStream(Stream s);
    }

    public abstract class DHCPOptionBase : IDHCPOption
    {
        protected TDHCPOption m_OptionType;
        private bool m_ZeroTerminatedStrings;

        public TDHCPOption OptionType
        {
            get
            {
                return m_OptionType;
            }
        }

        public bool ZeroTerminatedStrings
        {
            get
            {
                return m_ZeroTerminatedStrings;
            }
            set
            {
                m_ZeroTerminatedStrings = value;
            }
        }

        public abstract IDHCPOption FromStream(Stream s);
        public abstract void ToStream(Stream s);

        public DHCPOptionBase(TDHCPOption optionType)
        {
            m_OptionType = optionType;
        }
    }

    public class DHCPOptionParameterRequestList : DHCPOptionBase
    {
        private List<TDHCPOption> m_RequestList = new List<TDHCPOption>();

        #region IDHCPOption Members

        public List<TDHCPOption> RequestList
        {
            get
            {
                return m_RequestList;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionParameterRequestList result = new DHCPOptionParameterRequestList();
            while(true)
            {
                int c = s.ReadByte();
                if(c<0) break;
                result.m_RequestList.Add((TDHCPOption)c);
            }
            return result;
        }

        public override void ToStream(Stream s)
        {
            foreach(TDHCPOption opt in m_RequestList)
            {
                s.WriteByte((byte)opt);
            }
        }

        #endregion

        public DHCPOptionParameterRequestList()
            : base(TDHCPOption.ParameterRequestList)
        {
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(TDHCPOption opt in m_RequestList)
            {
                sb.Append(opt.ToString());
                sb.Append(",");
            }
            if(m_RequestList.Count>0) sb.Remove(sb.Length-1,1);
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, sb.ToString());
        }
    }

    public class DHCPOptionHostName : DHCPOptionBase
    {
        private string m_HostName;

        #region IDHCPOption Members

        public string HostName
        {
            get
            {
                return m_HostName;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionHostName result = new DHCPOptionHostName();
            result.m_HostName = ParseHelper.ReadString(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, ZeroTerminatedStrings, m_HostName);
        }

        #endregion

        public DHCPOptionHostName()
            : base(TDHCPOption.HostName)
        {
        }

        public DHCPOptionHostName(string hostName)
            : base(TDHCPOption.HostName)
        {
            m_HostName = hostName;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_HostName);
        }
    }


    public class DHCPOptionMessage : DHCPOptionBase
    {
        private string m_Message;

        #region IDHCPOption Members

        public string Message
        {
            get
            {
                return m_Message;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMessage result = new DHCPOptionMessage();
            result.m_Message = ParseHelper.ReadString(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, ZeroTerminatedStrings, m_Message);
        }

        #endregion

        public DHCPOptionMessage()
            : base(TDHCPOption.Message)
        {
        }

        public DHCPOptionMessage(string message)
            : base(TDHCPOption.Message)
        {
            m_Message = message;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_Message);
        }
    }

    public class DHCPOptionTFTPServerName : DHCPOptionBase
    {
        private string m_Name;

        #region IDHCPOption Members

        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionTFTPServerName result = new DHCPOptionTFTPServerName();
            result.m_Name = ParseHelper.ReadString(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, ZeroTerminatedStrings, m_Name);
        }

        #endregion

        public DHCPOptionTFTPServerName()
            : base(TDHCPOption.TFTPServerName)
        {
        }

        public DHCPOptionTFTPServerName(string name)
            : base(TDHCPOption.TFTPServerName)
        {
            m_Name = name;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_Name);
        }
    }

    public class DHCPOptionBootFileName : DHCPOptionBase
    {
        private string m_Name;

        #region IDHCPOption Members

        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionBootFileName result = new DHCPOptionBootFileName();
            result.m_Name = ParseHelper.ReadString(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, ZeroTerminatedStrings, m_Name);
        }

        #endregion

        public DHCPOptionBootFileName()
            : base(TDHCPOption.BootFileName)
        {
        }

        public DHCPOptionBootFileName(string name)
            : base(TDHCPOption.BootFileName)
        {
            m_Name = name;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_Name);
        }
    }

    public class DHCPOptionOptionOverload : DHCPOptionBase
    {
        private byte m_Overload;

        #region IDHCPOption Members

        public byte Overload
        {
            get
            {
                return m_Overload;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionOptionOverload result = new DHCPOptionOptionOverload();
            if (s.Length != 1) throw new IOException("Invalid DHCP option length");
            result.m_Overload = (byte)s.ReadByte();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteByte(m_Overload);
        }

        #endregion

        public DHCPOptionOptionOverload()
            : base(TDHCPOption.OptionOverload)
        {
        }

        public DHCPOptionOptionOverload(byte overload)
            : base(TDHCPOption.OptionOverload)
        {
            m_Overload = overload;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_Overload);
        }
    }

    public class DHCPOptionMaximumDHCPMessageSize : DHCPOptionBase
    {
        private ushort m_MaxSize;

        #region IDHCPOption Members

        public ushort MaxSize
        {
            get
            {
                return m_MaxSize;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMaximumDHCPMessageSize result = new DHCPOptionMaximumDHCPMessageSize();
            if (s.Length != 2) throw new IOException("Invalid DHCP option length");
            result.m_MaxSize = ParseHelper.ReadUInt16(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteUInt16(s,m_MaxSize);
        }

        #endregion

        public DHCPOptionMaximumDHCPMessageSize()
            : base(TDHCPOption.MaximumDHCPMessageSize)
        {
        }

        public DHCPOptionMaximumDHCPMessageSize(ushort maxSize)
            : base(TDHCPOption.MaximumDHCPMessageSize)
        {
            m_MaxSize = maxSize;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_MaxSize);
        }
    }

    public class DHCPOptionMessageType : DHCPOptionBase
    {
        private TDHCPMessageType m_MessageType;

        #region IDHCPOption Members

        public TDHCPMessageType MessageType
        {
            get
            {
                return m_MessageType;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMessageType result = new DHCPOptionMessageType();
            if (s.Length != 1) throw new IOException("Invalid DHCP option length");
            result.m_MessageType = (TDHCPMessageType)s.ReadByte();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteByte((byte)m_MessageType);
        }

        #endregion

        public DHCPOptionMessageType()
            : base(TDHCPOption.MessageType)
        {
        }

        public DHCPOptionMessageType(TDHCPMessageType messageType)
            : base(TDHCPOption.MessageType)
        {
            m_MessageType = messageType;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_MessageType);
        }
    }


    public class DHCPOptionServerIdentifier : DHCPOptionBase
    {
        private IPAddress m_IPAddress;

        #region IDHCPOption Members

        public IPAddress IPAddress
        {
            get
            {
                return m_IPAddress;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionServerIdentifier result = new DHCPOptionServerIdentifier();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_IPAddress = ParseHelper.ReadIPAddress(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteIPAddress(s, m_IPAddress);
        }

        #endregion

        public DHCPOptionServerIdentifier()
            : base(TDHCPOption.ServerIdentifier)
        {
        }

        public DHCPOptionServerIdentifier(IPAddress ipAddress)
            : base(TDHCPOption.ServerIdentifier)
        {
            m_IPAddress = ipAddress;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_IPAddress.ToString());
        }
    }


    public class DHCPOptionRequestedIPAddress : DHCPOptionBase
    {
        private IPAddress m_IPAddress;

        #region IDHCPOption Members

        public IPAddress IPAddress
        {
            get
            {
                return m_IPAddress;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionRequestedIPAddress result = new DHCPOptionRequestedIPAddress();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_IPAddress = ParseHelper.ReadIPAddress(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteIPAddress(s, m_IPAddress);
        }

        #endregion

        public DHCPOptionRequestedIPAddress()
            : base(TDHCPOption.RequestedIPAddress)
        {
        }

        public DHCPOptionRequestedIPAddress(IPAddress ipAddress)
            : base(TDHCPOption.RequestedIPAddress)
        {
            m_IPAddress = ipAddress;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_IPAddress.ToString());
        }
    }


    public class DHCPOptionSubnetMask : DHCPOptionBase
    {
        private IPAddress m_SubnetMask;

        #region IDHCPOption Members

        public IPAddress SubnetMask
        {
            get
            {
                return m_SubnetMask;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionSubnetMask result = new DHCPOptionSubnetMask();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_SubnetMask = ParseHelper.ReadIPAddress(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteIPAddress(s, m_SubnetMask);
        }

        #endregion

        public DHCPOptionSubnetMask()
            : base(TDHCPOption.SubnetMask)
        {
        }

        public DHCPOptionSubnetMask(IPAddress subnetMask)
            : base(TDHCPOption.SubnetMask)
        {
            m_SubnetMask = subnetMask;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_SubnetMask.ToString());
        }
    }


    public class DHCPOptionIPAddressLeaseTime : DHCPOptionBase
    {
        private TimeSpan m_LeaseTime;

        #region IDHCPOption Members

        public TimeSpan LeaseTime
        {
            get
            {
                return m_LeaseTime;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionIPAddressLeaseTime result = new DHCPOptionIPAddressLeaseTime();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_LeaseTime = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteUInt32(s, (uint)m_LeaseTime.TotalSeconds);
        }

        #endregion

        public DHCPOptionIPAddressLeaseTime()
            : base(TDHCPOption.IPAddressLeaseTime)
        {
        }

        public DHCPOptionIPAddressLeaseTime(TimeSpan leaseTime)
            : base(TDHCPOption.IPAddressLeaseTime)
        {
            m_LeaseTime = leaseTime;
            if (m_LeaseTime > Utils.InfiniteTimeSpan)
            {
                m_LeaseTime = Utils.InfiniteTimeSpan;
            }
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_LeaseTime == Utils.InfiniteTimeSpan ? "Infinite" : m_LeaseTime.ToString());
        }
    }

    public class DHCPOptionRenewalTimeValue : DHCPOptionBase
    {
        private TimeSpan m_TimeSpan;

        #region IDHCPOption Members

        public TimeSpan TimeSpan
        {
            get
            {
                return m_TimeSpan;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionRenewalTimeValue result = new DHCPOptionRenewalTimeValue();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_TimeSpan = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteUInt32(s, (uint)m_TimeSpan.TotalSeconds);
        }

        #endregion

        public DHCPOptionRenewalTimeValue()
            : base(TDHCPOption.RenewalTimeValue)
        {
        }

        public DHCPOptionRenewalTimeValue(TimeSpan timeSpan)
            : base(TDHCPOption.RenewalTimeValue)
        {
            m_TimeSpan = timeSpan;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_TimeSpan.ToString());
        }
    }

    public class DHCPOptionRebindingTimeValue : DHCPOptionBase
    {
        private TimeSpan m_TimeSpan;

        #region IDHCPOption Members

        public TimeSpan TimeSpan
        {
            get
            {
                return m_TimeSpan;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionRebindingTimeValue result = new DHCPOptionRebindingTimeValue();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_TimeSpan = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteUInt32(s, (uint)m_TimeSpan.TotalSeconds);
        }

        #endregion

        public DHCPOptionRebindingTimeValue()
            : base(TDHCPOption.RebindingTimeValue)
        {
        }

        public DHCPOptionRebindingTimeValue(TimeSpan timeSpan)
            : base(TDHCPOption.RebindingTimeValue)
        {
            m_TimeSpan = timeSpan;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_TimeSpan.ToString());
        }
    }

    public class DHCPOptionGeneric : DHCPOptionBase
    {
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionGeneric result = new DHCPOptionGeneric(m_OptionType);
            result.m_Data = new byte[s.Length];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionGeneric(TDHCPOption option) : base(option)
        {
            m_Data = new byte[0];
        }

        public DHCPOptionGeneric(TDHCPOption option, byte[] data) : base(option)
        {
            m_Data = data;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", m_OptionType, Utils.BytesToHexString(m_Data," "));
        }
    }

    public class DHCPOptionFullyQualifiedDomainName : DHCPOptionBase
    {
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionFullyQualifiedDomainName result = new DHCPOptionFullyQualifiedDomainName();
            result.m_Data = new byte[s.Length];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionFullyQualifiedDomainName()
            : base(TDHCPOption.FullyQualifiedDomainName)
        {
            m_Data = new byte[0];
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, Utils.BytesToHexString(m_Data, " "));
        }
    }

    public class DHCPOptionVendorClassIdentifier : DHCPOptionBase
    {
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionVendorClassIdentifier result = new DHCPOptionVendorClassIdentifier();
            result.m_Data = new byte[s.Length];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionVendorClassIdentifier()
            : base(TDHCPOption.VendorClassIdentifier)
        {
            m_Data = new byte[0];
        }

        public DHCPOptionVendorClassIdentifier(byte[] data)
            : base(TDHCPOption.VendorClassIdentifier)
        {
            m_Data = data;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, Utils.BytesToHexString(m_Data," "));
        }
    }

    public class DHCPOptionClientIdentifier : DHCPOptionBase
    {
        private DHCPMessage.THardwareType m_HardwareType;
        private byte[] m_Data;

        public DHCPMessage.THardwareType HardwareType
        {
            get { return m_HardwareType; }
            set { m_HardwareType = value; }   
        }

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionClientIdentifier result = new DHCPOptionClientIdentifier();
            m_HardwareType = (DHCPMessage.THardwareType)ParseHelper.ReadUInt8(s);
            result.m_Data = new byte[s.Length - s.Position];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteUInt8(s, (byte)m_HardwareType);
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionClientIdentifier()
            : base(TDHCPOption.ClientIdentifier)
        {
            m_HardwareType = DHCPMessage.THardwareType.Unknown;
            m_Data = new byte[0];
        }

        public DHCPOptionClientIdentifier(DHCPMessage.THardwareType hardwareType,byte[] data)
            : base(TDHCPOption.ClientIdentifier)
        {
            m_HardwareType = hardwareType;
            m_Data = data;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],htype=[{1}],value=[{2}])", OptionType, m_HardwareType, Utils.BytesToHexString(m_Data," "));
        }
    }

    public class DHCPOptionVendorSpecificInformation : DHCPOptionBase
    {
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionVendorSpecificInformation result = new DHCPOptionVendorSpecificInformation();
            result.m_Data = new byte[s.Length];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionVendorSpecificInformation()
            : base(TDHCPOption.VendorSpecificInformation)
        {
            m_Data = new byte[0];
        }

        public DHCPOptionVendorSpecificInformation(byte[] data)
            : base(TDHCPOption.VendorSpecificInformation)
        {
            m_Data = data;
        }

        public DHCPOptionVendorSpecificInformation(string data)
            : base(TDHCPOption.VendorSpecificInformation)
        {
            MemoryStream ms = new MemoryStream();
            ParseHelper.WriteString(ms, ZeroTerminatedStrings, data);
            ms.Flush();
            m_Data = ms.ToArray();
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, Utils.BytesToHexString(m_Data, " "));
        }
    }

    public class DHCPOptionFixedLength : DHCPOptionBase
    {
        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            return this;
        }

        public override void ToStream(Stream s)
        {
        }

        #endregion

        public DHCPOptionFixedLength(TDHCPOption option) : base(option)
        {
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[])", m_OptionType);
        }
    }
}
