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
        TDHCPOption OptionType { get; }
        IDHCPOption FromStream(Stream s);
        void ToStream(Stream s);
    }

    public class DHCPOptionParameterRequestList : IDHCPOption
    {
        private List<TDHCPOption> m_RequestList = new List<TDHCPOption>();

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.ParameterRequestList;
            }
        }

        public List<TDHCPOption> RequestList
        {
            get
            {
                return m_RequestList;
            }
        }

        public IDHCPOption FromStream(Stream s)
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

        public void ToStream(Stream s)
        {
            foreach(TDHCPOption opt in m_RequestList)
            {
                s.WriteByte((byte)opt);
            }
        }

        #endregion

        public DHCPOptionParameterRequestList()
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

    public class DHCPOptionHostName : IDHCPOption
    {
        private string m_HostName;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.HostName;
            }
        }

        public string HostName
        {
            get
            {
                return m_HostName;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionHostName result = new DHCPOptionHostName();
            result.m_HostName = ParseHelper.ReadString(s);
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, m_HostName);
        }

        #endregion

        public DHCPOptionHostName()
        {
        }

        public DHCPOptionHostName(string hostName)
        {
            m_HostName = hostName;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_HostName);
        }
    }


    public class DHCPOptionMessage : IDHCPOption
    {
        private string m_Message;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.Message;
            }
        }

        public string Message
        {
            get
            {
                return m_Message;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMessage result = new DHCPOptionMessage();
            result.m_Message = ParseHelper.ReadString(s);
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, m_Message);
        }

        #endregion

        public DHCPOptionMessage()
        {
        }

        public DHCPOptionMessage(string message)
        {
            m_Message = message;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_Message);
        }
    }

    public class DHCPOptionTFTPServerName : IDHCPOption
    {
        private string m_Name;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.TFTPServerName;
            }
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionTFTPServerName result = new DHCPOptionTFTPServerName();
            result.m_Name = ParseHelper.ReadString(s);
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteString(s,m_Name);
        }

        #endregion

        public DHCPOptionTFTPServerName()
        {
        }

        public DHCPOptionTFTPServerName(string name)
        {
            m_Name = name;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_Name);
        }
    }

    public class DHCPOptionBootFileName : IDHCPOption
    {
        private string m_Name;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.BootFileName;
            }
        }

        public string Name
        {
            get
            {
                return m_Name;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionBootFileName result = new DHCPOptionBootFileName();
            result.m_Name = ParseHelper.ReadString(s);
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, m_Name);
        }

        #endregion

        public DHCPOptionBootFileName()
        {
        }

        public DHCPOptionBootFileName(string name)
        {
            m_Name = name;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_Name);
        }
    }

    public class DHCPOptionOptionOverload : IDHCPOption
    {
        private byte m_Overload;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.OptionOverload;
            }
        }

        public byte Overload
        {
            get
            {
                return m_Overload;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionOptionOverload result = new DHCPOptionOptionOverload();
            if (s.Length != 1) throw new IOException("Invalid DHCP option length");
            result.m_Overload = (byte)s.ReadByte();
            return result;
        }

        public void ToStream(Stream s)
        {
            s.WriteByte(m_Overload);
        }

        #endregion

        public DHCPOptionOptionOverload()
        {
        }

        public DHCPOptionOptionOverload(byte overload)
        {
            m_Overload = overload;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_Overload);
        }
    }

    public class DHCPOptionMaximumDHCPMessageSize : IDHCPOption
    {
        private ushort m_MaxSize;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.MaximumDHCPMessageSize;
            }
        }

        public ushort MaxSize
        {
            get
            {
                return m_MaxSize;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMaximumDHCPMessageSize result = new DHCPOptionMaximumDHCPMessageSize();
            if (s.Length != 2) throw new IOException("Invalid DHCP option length");
            result.m_MaxSize = ParseHelper.ReadUInt16(s);
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteUInt16(s,m_MaxSize);
        }

        #endregion

        public DHCPOptionMaximumDHCPMessageSize()
        {
        }

        public DHCPOptionMaximumDHCPMessageSize(ushort maxSize)
        {
            m_MaxSize = maxSize;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_MaxSize);
        }
    }

    public class DHCPOptionMessageType : IDHCPOption
    {
        private TDHCPMessageType m_MessageType;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.MessageType;
            }
        }

        public TDHCPMessageType MessageType
        {
            get
            {
                return m_MessageType;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMessageType result = new DHCPOptionMessageType();
            if (s.Length != 1) throw new IOException("Invalid DHCP option length");
            result.m_MessageType = (TDHCPMessageType)s.ReadByte();
            return result;
        }

        public void ToStream(Stream s)
        {
            s.WriteByte((byte)m_MessageType);
        }

        #endregion

        public DHCPOptionMessageType()
        {
        }

        public DHCPOptionMessageType(TDHCPMessageType messageType)
        {
            m_MessageType = messageType;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_MessageType);
        }
    }


    public class DHCPOptionServerIdentifier : IDHCPOption
    {
        private IPAddress m_IPAddress;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.ServerIdentifier;
            }
        }

        public IPAddress IPAddress
        {
            get
            {
                return m_IPAddress;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionServerIdentifier result = new DHCPOptionServerIdentifier();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_IPAddress = ParseHelper.ReadIPAddress(s);
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteIPAddress(s, m_IPAddress);
        }

        #endregion

        public DHCPOptionServerIdentifier()
        {
        }

           public DHCPOptionServerIdentifier(IPAddress ipAddress)
        {
            m_IPAddress = ipAddress;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_IPAddress.ToString());
        }
    }


    public class DHCPOptionRequestedIPAddress : IDHCPOption
    {
        private IPAddress m_IPAddress;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.RequestedIPAddress;
            }
        }

        public IPAddress IPAddress
        {
            get
            {
                return m_IPAddress;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionRequestedIPAddress result = new DHCPOptionRequestedIPAddress();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_IPAddress = ParseHelper.ReadIPAddress(s);
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteIPAddress(s, m_IPAddress);
        }

        #endregion

        public DHCPOptionRequestedIPAddress()
        {
        }

        public DHCPOptionRequestedIPAddress(IPAddress ipAddress)
        {
            m_IPAddress = ipAddress;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_IPAddress.ToString());
        }
    }


    public class DHCPOptionSubnetMask : IDHCPOption
    {
        private IPAddress m_SubnetMask;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.SubnetMask;
            }
        }

        public IPAddress SubnetMask
        {
            get
            {
                return m_SubnetMask;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionSubnetMask result = new DHCPOptionSubnetMask();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_SubnetMask = ParseHelper.ReadIPAddress(s);
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteIPAddress(s, m_SubnetMask);
        }

        #endregion

        public DHCPOptionSubnetMask()
        {
        }

        public DHCPOptionSubnetMask(IPAddress subnetMask)
        {
            m_SubnetMask = subnetMask;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_SubnetMask.ToString());
        }
    }


    public class DHCPOptionIPAddressLeaseTime : IDHCPOption
    {
        private TimeSpan m_LeaseTime;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.IPAddressLeaseTime;
            }
        }

        public TimeSpan LeaseTime
        {
            get
            {
                return m_LeaseTime;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionIPAddressLeaseTime result = new DHCPOptionIPAddressLeaseTime();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_LeaseTime = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteUInt32(s, (uint)m_LeaseTime.TotalSeconds);
        }

        #endregion

        public DHCPOptionIPAddressLeaseTime()
        {
        }

        public DHCPOptionIPAddressLeaseTime(TimeSpan leaseTime)
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

    public class DHCPOptionRenewalTimeValue : IDHCPOption
    {
        private TimeSpan m_TimeSpan;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.RenewalTimeValue;
            }
        }

        public TimeSpan TimeSpan
        {
            get
            {
                return m_TimeSpan;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionRenewalTimeValue result = new DHCPOptionRenewalTimeValue();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_TimeSpan = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteUInt32(s, (uint)m_TimeSpan.TotalSeconds);
        }

        #endregion

        public DHCPOptionRenewalTimeValue()
        {
        }

        public DHCPOptionRenewalTimeValue(TimeSpan timeSpan)
        {
            m_TimeSpan = timeSpan;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_TimeSpan.ToString());
        }
    }

    public class DHCPOptionRebindingTimeValue : IDHCPOption
    {
        private TimeSpan m_TimeSpan;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.RebindingTimeValue;
            }
        }

        public TimeSpan TimeSpan
        {
            get
            {
                return m_TimeSpan;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionRebindingTimeValue result = new DHCPOptionRebindingTimeValue();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_TimeSpan = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteUInt32(s, (uint)m_TimeSpan.TotalSeconds);
        }

        #endregion

        public DHCPOptionRebindingTimeValue()
        {
        }

        public DHCPOptionRebindingTimeValue(TimeSpan timeSpan)
        {
            m_TimeSpan = timeSpan;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_TimeSpan.ToString());
        }
    }

    public class DHCPOptionGeneric : IDHCPOption
    {
        private TDHCPOption m_Option;
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return m_Option;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionGeneric result = new DHCPOptionGeneric(m_Option);
            result.m_Data = new byte[s.Length];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public void ToStream(Stream s)
        {
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionGeneric(TDHCPOption option)
        {
            m_Option = option;
            m_Data = new byte[0];
        }

        public DHCPOptionGeneric(TDHCPOption option, byte[] data)
        {
            m_Option = option;
            m_Data = data;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", m_Option, Utils.BytesToHexString(m_Data," "));
        }
    }

    public class DHCPOptionFullyQualifiedDomainName : IDHCPOption
    {
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.FullyQualifiedDomainName;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionFullyQualifiedDomainName result = new DHCPOptionFullyQualifiedDomainName();
            result.m_Data = new byte[s.Length];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public void ToStream(Stream s)
        {
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionFullyQualifiedDomainName()
        {
            m_Data = new byte[0];
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, Utils.BytesToHexString(m_Data, " "));
        }
    }

    public class DHCPOptionVendorClassIdentifier : IDHCPOption
    {
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.VendorClassIdentifier;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionVendorClassIdentifier result = new DHCPOptionVendorClassIdentifier();
            result.m_Data = new byte[s.Length];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public void ToStream(Stream s)
        {
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionVendorClassIdentifier()
        {
            m_Data = new byte[0];
        }

        public DHCPOptionVendorClassIdentifier(byte[] data)
        {
            m_Data = data;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, Utils.BytesToHexString(m_Data," "));
        }
    }

    public class DHCPOptionClientIdentifier : IDHCPOption
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

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.ClientIdentifier;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionClientIdentifier result = new DHCPOptionClientIdentifier();
            m_HardwareType = (DHCPMessage.THardwareType)ParseHelper.ReadUInt8(s);
            result.m_Data = new byte[s.Length - s.Position];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public void ToStream(Stream s)
        {
            ParseHelper.WriteUInt8(s, (byte)m_HardwareType);
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionClientIdentifier()
        {
            m_HardwareType = DHCPMessage.THardwareType.Unknown;
            m_Data = new byte[0];
        }

        public DHCPOptionClientIdentifier(DHCPMessage.THardwareType hardwareType,byte[] data)
        {
            m_HardwareType = hardwareType;
            m_Data = data;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],htype=[{1}],value=[{2}])", OptionType, m_HardwareType, Utils.BytesToHexString(m_Data," "));
        }
    }

    public class DHCPOptionVendorSpecificInformation : IDHCPOption
    {
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return TDHCPOption.VendorSpecificInformation;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            DHCPOptionVendorSpecificInformation result = new DHCPOptionVendorSpecificInformation();
            result.m_Data = new byte[s.Length];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public void ToStream(Stream s)
        {
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionVendorSpecificInformation()
        {
            m_Data = new byte[0];
        }

        public DHCPOptionVendorSpecificInformation(byte[] data)
        {
            m_Data = data;
        }

        public DHCPOptionVendorSpecificInformation(string data)
        {
            MemoryStream ms = new MemoryStream();
            ParseHelper.WriteString(ms, data);
            ms.Flush();
            m_Data = ms.ToArray();
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, Utils.BytesToHexString(m_Data, " "));
        }
    }

    public class DHCPOptionFixedLength : IDHCPOption
    {
        private TDHCPOption m_Option;

        #region IDHCPOption Members

        public TDHCPOption OptionType
        {
            get
            {
                return m_Option;
            }
        }

        public IDHCPOption FromStream(Stream s)
        {
            return this;
        }

        public void ToStream(Stream s)
        {
        }

        #endregion

        public DHCPOptionFixedLength(TDHCPOption option)
        {
            m_Option = option;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[])", m_Option);
        }
    }
}
