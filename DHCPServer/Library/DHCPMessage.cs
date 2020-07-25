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
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPMessage
    {
        private static IDHCPOption[] optionsTemplates;

        public enum TOpcode
        {
            Unknown = 0,
            BootRequest = 1,
            BootReply = 2
        }

        public enum THardwareType
        {
            Unknown = 0,
            Ethernet = 1,
            Experimental_Ethernet = 2,
            Amateur_Radio_AX_25 = 3,
            Proteon_ProNET_Token_Ring = 4,
            Chaos = 5,
            IEEE_802_Networks = 6,
            ARCNET = 7,
            Hyperchannel = 8,
            Lanstar = 9,
            Autonet_Short_Address = 10,
            LocalTalk = 11,
            LocalNet = 12,
            Ultra_link = 13,
            SMDS = 14,
            Frame_Relay = 15,
            Asynchronous_Transmission_Mode1 = 16,
            HDLC = 17,
            Fibre_Channel = 18,
            Asynchronous_Transmission_Mode2 = 19,
            Serial_Line = 20,
            Asynchronous_Transmission_Mode3 = 21,
        };

        private TOpcode m_Opcode;
        private THardwareType m_HardwareType;
        private byte m_Hops;
        private uint m_XID;
        private ushort m_Secs;
        private bool m_BroadCast;
        private IPAddress m_ClientIPAddress;
        private IPAddress m_YourIPAddress;
        private IPAddress m_NextServerIPAddress;
        private IPAddress m_RelayAgentIPAddress;
        private byte[] m_ClientHardwareAddress;
        private string m_ServerHostName;
        private string m_BootFileName;
        private List<IDHCPOption> m_Options;

        public TOpcode Opcode
        {
            get{ return m_Opcode; }
            set{ m_Opcode = value; }
        }

        public THardwareType HardwareType
        {
            get { return m_HardwareType; }
            set { m_HardwareType = value; }
        }

        public byte Hops
        {
            get { return m_Hops; }
            set { m_Hops = value; }
        }

        public uint XID
        {
            get { return m_XID; }
            set { m_XID = value; }
        }

        public ushort Secs
        {
            get { return m_Secs; }
            set { m_Secs = value; }
        }

        public bool BroadCast
        {
            get { return m_BroadCast; }
            set { m_BroadCast = value; }
        }

        public IPAddress ClientIPAddress
        {
            get { return m_ClientIPAddress; }
            set { m_ClientIPAddress = value; }
        }

        public IPAddress YourIPAddress
        {
            get { return m_YourIPAddress; }
            set { m_YourIPAddress = value; }
        }

        public IPAddress NextServerIPAddress
        {
            get { return m_NextServerIPAddress; }
            set { m_NextServerIPAddress = value; }
        }

        public IPAddress RelayAgentIPAddress
        {
            get { return m_RelayAgentIPAddress; }
            set { m_RelayAgentIPAddress = value; }
        }

        public byte[] ClientHardwareAddress
        {
            get { return m_ClientHardwareAddress; }
            set { m_ClientHardwareAddress = value; }
        }

        public string ServerHostName
        {
            get { return m_ServerHostName; }
            set { m_ServerHostName = value; }
        }

        public string BootFileName
        {
            get { return m_BootFileName; }
            set { m_BootFileName = value; }
        }

        public List<IDHCPOption> Options
        {
            get { return m_Options; }
            set { m_Options = value; }
        }

        /// <summary>
        /// Convenience property to easily get or set the messagetype option
        /// </summary>
        public TDHCPMessageType MessageType
        {
            get
            {
                DHCPOptionMessageType messageTypeDHCPOption = (DHCPOptionMessageType)GetOption(TDHCPOption.MessageType);
                if (messageTypeDHCPOption != null)
                {
                    return messageTypeDHCPOption.MessageType;
                }
                else
                {
                    return TDHCPMessageType.Undefined;
                }
            }
            set
            {
                TDHCPMessageType currentMessageType = MessageType;
                if(currentMessageType!=value)
                {
                    m_Options.Add(new DHCPOptionMessageType(value));
                }
            }
        }

        static DHCPMessage()
        {
            optionsTemplates = new IDHCPOption[256];
            for (int t = 1; t < 255; t++)
            {
                optionsTemplates[t] = new DHCPOptionGeneric((TDHCPOption)t);
            }
            optionsTemplates[0] = new DHCPOptionFixedLength(TDHCPOption.Pad);
            optionsTemplates[255] = new DHCPOptionFixedLength(TDHCPOption.End);
            optionsTemplates[(int)TDHCPOption.HostName] = new DHCPOptionHostName();
            optionsTemplates[(int)TDHCPOption.IPAddressLeaseTime] = new DHCPOptionIPAddressLeaseTime();
            optionsTemplates[(int)TDHCPOption.ServerIdentifier] = new DHCPOptionServerIdentifier();
            optionsTemplates[(int)TDHCPOption.RequestedIPAddress] = new DHCPOptionRequestedIPAddress();
            optionsTemplates[(int)TDHCPOption.OptionOverload] = new DHCPOptionOptionOverload();
            optionsTemplates[(int)TDHCPOption.TFTPServerName] = new DHCPOptionTFTPServerName();
            optionsTemplates[(int)TDHCPOption.BootFileName] = new DHCPOptionBootFileName();
            optionsTemplates[(int)TDHCPOption.MessageType] = new DHCPOptionMessageType();
            optionsTemplates[(int)TDHCPOption.Message] = new DHCPOptionMessage();
            optionsTemplates[(int)TDHCPOption.MaximumDHCPMessageSize] = new DHCPOptionMaximumDHCPMessageSize();
            optionsTemplates[(int)TDHCPOption.ParameterRequestList] = new DHCPOptionParameterRequestList();
            optionsTemplates[(int)TDHCPOption.RenewalTimeValue] = new DHCPOptionRenewalTimeValue();
            optionsTemplates[(int)TDHCPOption.RebindingTimeValue] = new DHCPOptionRebindingTimeValue();
            optionsTemplates[(int)TDHCPOption.VendorClassIdentifier] = new DHCPOptionVendorClassIdentifier();
            optionsTemplates[(int)TDHCPOption.ClientIdentifier] = new DHCPOptionClientIdentifier();
            optionsTemplates[(int)TDHCPOption.FullyQualifiedDomainName] = new DHCPOptionFullyQualifiedDomainName();
            optionsTemplates[(int)TDHCPOption.SubnetMask] = new DHCPOptionSubnetMask();
        }

        public DHCPMessage()
        {
            m_HardwareType = THardwareType.Ethernet;
            m_ClientIPAddress = IPAddress.Any;
            m_YourIPAddress  = IPAddress.Any;
            m_NextServerIPAddress  = IPAddress.Any;
            m_RelayAgentIPAddress  = IPAddress.Any;
            m_ClientHardwareAddress = new byte[0];
            m_ServerHostName = "";
            m_BootFileName = "";
            m_Options = new List<IDHCPOption>();
        }

        public IDHCPOption GetOption(TDHCPOption optionType)
        {
            return m_Options.Find(delegate(IDHCPOption v) { return v.OptionType == optionType; });
        }

        public bool IsRequestedParameter(TDHCPOption optionType)
        {
            DHCPOptionParameterRequestList dhcpOptionParameterRequestList = (DHCPOptionParameterRequestList)GetOption(TDHCPOption.ParameterRequestList);
            return (dhcpOptionParameterRequestList != null && dhcpOptionParameterRequestList.RequestList.Contains(optionType));
        }

        private DHCPMessage(Stream s) : this()
        {
            m_Opcode = (TOpcode)s.ReadByte();
            m_HardwareType = (THardwareType)s.ReadByte();
            m_ClientHardwareAddress = new byte[s.ReadByte()];
            m_Hops = (byte)s.ReadByte();
            m_XID = ParseHelper.ReadUInt32(s);
            m_Secs = ParseHelper.ReadUInt16(s);
            m_BroadCast = ((ParseHelper.ReadUInt16(s) & 0x8000) == 0x8000);
            m_ClientIPAddress = ParseHelper.ReadIPAddress(s);
            m_YourIPAddress = ParseHelper.ReadIPAddress(s);
            m_NextServerIPAddress = ParseHelper.ReadIPAddress(s);
            m_RelayAgentIPAddress = ParseHelper.ReadIPAddress(s);
            s.Read(m_ClientHardwareAddress, 0, m_ClientHardwareAddress.Length);
            for (int t = m_ClientHardwareAddress.Length; t < 16; t++) s.ReadByte();

            byte[] serverHostNameBuffer = new byte[64];
            s.Read(serverHostNameBuffer, 0, serverHostNameBuffer.Length);

            byte[] bootFileNameBuffer = new byte[128];
            s.Read(bootFileNameBuffer, 0, bootFileNameBuffer.Length);

            // read options magic cookie
            if(s.ReadByte() != 99) throw new IOException();
            if(s.ReadByte() != 130) throw new IOException();
            if(s.ReadByte() != 83) throw new IOException();
            if(s.ReadByte() != 99) throw new IOException();

            byte[] optionsBuffer = new byte[s.Length - s.Position];
            s.Read(optionsBuffer, 0, optionsBuffer.Length);

            byte overload = ScanOverload(new MemoryStream(optionsBuffer));

            switch (overload)
            {
                default:
                    m_ServerHostName = ParseHelper.ReadZString(new MemoryStream(serverHostNameBuffer));
                    m_BootFileName = ParseHelper.ReadZString(new MemoryStream(bootFileNameBuffer));
                    m_Options = ReadOptions(optionsBuffer, new byte[0], new byte[0]);
                    break;

                case 1:
                    m_ServerHostName = ParseHelper.ReadZString(new MemoryStream(serverHostNameBuffer));
                    m_Options = ReadOptions(optionsBuffer, bootFileNameBuffer, new byte[0]);
                    break;

                case 2:
                    m_BootFileName = ParseHelper.ReadZString(new MemoryStream(bootFileNameBuffer));
                    m_Options = ReadOptions(optionsBuffer, serverHostNameBuffer, new byte[0]);
                    break;

                case 3:
                    m_Options = ReadOptions(optionsBuffer, bootFileNameBuffer, serverHostNameBuffer );
                    break;
            }
        }

        private static List<IDHCPOption> ReadOptions(byte[] buffer1, byte[] buffer2, byte[] buffer3)
        {
            List<IDHCPOption> result = new List<IDHCPOption>();
            ReadOptions(result, new MemoryStream(buffer1, true), new MemoryStream(buffer2, true), new MemoryStream(buffer3, true));
            ReadOptions(result, new MemoryStream(buffer2, true), new MemoryStream(buffer3, true));
            ReadOptions(result, new MemoryStream(buffer3, true));
            return result;
        }

        private static void CopyBytes(Stream source, Stream target, int length)
        {
            byte[] buffer = new byte[length];
            source.Read(buffer, 0, length);
            target.Write(buffer, 0, length);
        }

        private static void ReadOptions(List<IDHCPOption> options,MemoryStream s,params MemoryStream[] spillovers)
        {
            while (true)
            {
                int code = s.ReadByte();
                if (code == -1 || code == 255) break;
                else if (code == 0) continue;
                else
                {
                    MemoryStream concatStream = new MemoryStream();
                    int len = s.ReadByte();
                    if (len == -1) break;
                    CopyBytes(s, concatStream, len);
                    AppendOverflow(code, s, concatStream);
                    foreach (MemoryStream spillOver in spillovers)
                    {
                        AppendOverflow(code, spillOver, concatStream);
                    }
                    concatStream.Position = 0;
                    options.Add(optionsTemplates[code].FromStream(concatStream));
                }
            }
        }

        private static void AppendOverflow(int code, MemoryStream source, MemoryStream target)
        {
            long initPosition = source.Position;
            try
            {
                while (true)
                {
                    int c = source.ReadByte();
                    if (c == -1 || c == 255) break;
                    else if (c == 0) continue;
                    else
                    {
                        int l = source.ReadByte();
                        if (l == -1) break;

                        if (c == code)
                        {
                            long startPosition = source.Position - 2;
                            CopyBytes(source, target, l);
                            source.Position = startPosition;
                            for (int t = 0; t < (l + 2); t++)
                            {
                                source.WriteByte(0);
                            }
                        }
                        else
                        {
                            source.Seek(l, SeekOrigin.Current);
                        }
                    }
                }
            }
            finally
            {
                source.Position = initPosition;
            }
        }

        /// <summary>
        /// Locate the overload option value in the passed stream.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Returns the overload option value, or 0 if it wasn't found</returns>
        private static byte ScanOverload(Stream s)
        {
            byte result = 0;

            while (true)
            {
                int code = s.ReadByte();
                if (code == -1 || code == 255) break;
                else if (code == 0) continue;
                else if (code == 52)
                {
                    if (s.ReadByte() != 1) throw new IOException("Invalid length of DHCP option 'Option Overload'");
                    result = (byte)s.ReadByte();
                }
                else
                {
                    int l = s.ReadByte();
                    if (l == -1) break;
                    s.Position += l;
                }
            }
            return result;
        }

        public static DHCPMessage FromStream(Stream s)
        {
            return new DHCPMessage(s);
        }

        public void ToStream(Stream s,int minimumPacketSize)
        {
            s.WriteByte((byte)m_Opcode);
            s.WriteByte((byte)m_HardwareType);
            s.WriteByte((byte)m_ClientHardwareAddress.Length);
            s.WriteByte((byte)m_Hops);
            ParseHelper.WriteUInt32(s, m_XID);
            ParseHelper.WriteUInt16(s, m_Secs);
            ParseHelper.WriteUInt16(s, m_BroadCast ? (ushort)0x8000 : (ushort)0x0);
            ParseHelper.WriteIPAddress(s, m_ClientIPAddress);
            ParseHelper.WriteIPAddress(s, m_YourIPAddress);
            ParseHelper.WriteIPAddress(s, m_NextServerIPAddress);
            ParseHelper.WriteIPAddress(s, m_RelayAgentIPAddress);
            s.Write(m_ClientHardwareAddress, 0, m_ClientHardwareAddress.Length);
            for(int t=m_ClientHardwareAddress.Length; t<16; t++) s.WriteByte(0);
            ParseHelper.WriteZString(s, m_ServerHostName, 64);  // BOOTP legacy
            ParseHelper.WriteZString(s, m_BootFileName, 128);   // BOOTP legacy
            s.Write(new byte[] { 99, 130, 83, 99 }, 0, 4);  // options magic cookie
            // write options
            foreach (IDHCPOption option in m_Options)
            {
                MemoryStream optionStream = new MemoryStream();
                option.ToStream(optionStream);
                s.WriteByte((byte)option.OptionType);
                s.WriteByte((byte)optionStream.Length);
                optionStream.Position = 0;
                CopyBytes(optionStream, s, (int)optionStream.Length);
            }
            // write end option
            s.WriteByte(255);
            s.Flush();

            while (s.Length < minimumPacketSize)
            {
                s.WriteByte(0);
            }

            s.Flush();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Opcode (op)                    : {0}\r\n", m_Opcode);
            sb.AppendFormat("HardwareType (htype)           : {0}\r\n", m_HardwareType);
            sb.AppendFormat("Hops                           : {0}\r\n", m_Hops);
            sb.AppendFormat("XID                            : {0}\r\n", m_XID);
            sb.AppendFormat("Secs                           : {0}\r\n", m_Secs);
            sb.AppendFormat("BroadCast (flags)              : {0}\r\n", m_BroadCast);
            sb.AppendFormat("ClientIPAddress (ciaddr)       : {0}\r\n", m_ClientIPAddress);
            sb.AppendFormat("YourIPAddress (yiaddr)         : {0}\r\n", m_YourIPAddress);
            sb.AppendFormat("NextServerIPAddress (siaddr)   : {0}\r\n", m_NextServerIPAddress);
            sb.AppendFormat("RelayAgentIPAddress (giaddr)   : {0}\r\n", m_RelayAgentIPAddress);
            sb.AppendFormat("ClientHardwareAddress (chaddr) : {0}\r\n", Utils.BytesToHexString(m_ClientHardwareAddress,"-"));
            sb.AppendFormat("ServerHostName (sname)         : {0}\r\n", m_ServerHostName);
            sb.AppendFormat("BootFileName (file)            : {0}\r\n", m_BootFileName);

            foreach(IDHCPOption option in m_Options)
            {
                sb.AppendFormat("Option                         : {0}\r\n", option.ToString());
            }

            return sb.ToString();
        }
    }
}
