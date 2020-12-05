using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPMessage
    {
        private static readonly IDHCPOption[] s_optionsTemplates;

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

        private TOpcode _opcode;
        private THardwareType _hardwareType;
        private byte _hops;
        private uint _xID;
        private ushort _secs;
        private bool _broadCast;
        private IPAddress _clientIPAddress;
        private IPAddress _yourIPAddress;
        private IPAddress _nextServerIPAddress;
        private IPAddress _relayAgentIPAddress;
        private byte[] _clientHardwareAddress;
        private string _serverHostName;
        private string _bootFileName;
        private List<IDHCPOption> _options;

        public TOpcode Opcode
        {
            get { return _opcode; }
            set { _opcode = value; }
        }

        public THardwareType HardwareType
        {
            get { return _hardwareType; }
            set { _hardwareType = value; }
        }

        public byte Hops
        {
            get { return _hops; }
            set { _hops = value; }
        }

        public uint XID
        {
            get { return _xID; }
            set { _xID = value; }
        }

        public ushort Secs
        {
            get { return _secs; }
            set { _secs = value; }
        }

        public bool BroadCast
        {
            get { return _broadCast; }
            set { _broadCast = value; }
        }

        public IPAddress ClientIPAddress
        {
            get { return _clientIPAddress; }
            set { _clientIPAddress = value; }
        }

        public IPAddress YourIPAddress
        {
            get { return _yourIPAddress; }
            set { _yourIPAddress = value; }
        }

        public IPAddress NextServerIPAddress
        {
            get { return _nextServerIPAddress; }
            set { _nextServerIPAddress = value; }
        }

        public IPAddress RelayAgentIPAddress
        {
            get { return _relayAgentIPAddress; }
            set { _relayAgentIPAddress = value; }
        }

        public byte[] ClientHardwareAddress
        {
            get { return _clientHardwareAddress; }
            set { _clientHardwareAddress = value; }
        }

        public string ServerHostName
        {
            get { return _serverHostName; }
            set { _serverHostName = value; }
        }

        public string BootFileName
        {
            get { return _bootFileName; }
            set { _bootFileName = value; }
        }

        public List<IDHCPOption> Options
        {
            get { return _options; }
            set { _options = value; }
        }

        /// <summary>
        /// Convenience property to easily get or set the messagetype option
        /// </summary>
        public TDHCPMessageType MessageType
        {
            get
            {
                DHCPOptionMessageType messageTypeDHCPOption = (DHCPOptionMessageType)GetOption(TDHCPOption.MessageType);
                if(messageTypeDHCPOption != null)
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
                if(currentMessageType != value)
                {
                    _options.Add(new DHCPOptionMessageType(value));
                }
            }
        }

        private static void RegisterOption(IDHCPOption option)
        {
            s_optionsTemplates[(int)option.OptionType] = option;
        }

        static DHCPMessage()
        {
            s_optionsTemplates = new IDHCPOption[256];
            for(int t = 1; t < 255; t++)
            {
                s_optionsTemplates[t] = new DHCPOptionGeneric((TDHCPOption)t);
            }

            RegisterOption(new DHCPOptionFixedLength(TDHCPOption.Pad));
            RegisterOption(new DHCPOptionFixedLength(TDHCPOption.End));
            RegisterOption(new DHCPOptionHostName());
            RegisterOption(new DHCPOptionIPAddressLeaseTime());
            RegisterOption(new DHCPOptionServerIdentifier());
            RegisterOption(new DHCPOptionRequestedIPAddress());
            RegisterOption(new DHCPOptionOptionOverload());
            RegisterOption(new DHCPOptionTFTPServerName());
            RegisterOption(new DHCPOptionBootFileName());
            RegisterOption(new DHCPOptionMessageType());
            RegisterOption(new DHCPOptionMessage());
            RegisterOption(new DHCPOptionMaximumDHCPMessageSize());
            RegisterOption(new DHCPOptionParameterRequestList());
            RegisterOption(new DHCPOptionRenewalTimeValue());
            RegisterOption(new DHCPOptionRebindingTimeValue());
            RegisterOption(new DHCPOptionVendorClassIdentifier());
            RegisterOption(new DHCPOptionClientIdentifier());
            RegisterOption(new DHCPOptionFullyQualifiedDomainName());
            RegisterOption(new DHCPOptionSubnetMask());
            RegisterOption(new DHCPOptionRouter());
            RegisterOption(new DHCPOptionDomainNameServer());
            RegisterOption(new DHCPOptionNetworkTimeProtocolServers());
#if RELAYAGENTINFORMATION
            RegisterOption(new DHCPOptionRelayAgentInformation());
#endif
        }

        public DHCPMessage()
        {
            _hardwareType = THardwareType.Ethernet;
            _clientIPAddress = IPAddress.Any;
            _yourIPAddress = IPAddress.Any;
            _nextServerIPAddress = IPAddress.Any;
            _relayAgentIPAddress = IPAddress.Any;
            _clientHardwareAddress = new byte[0];
            _serverHostName = "";
            _bootFileName = "";
            _options = new List<IDHCPOption>();
        }

        public IDHCPOption GetOption(TDHCPOption optionType)
        {
            return _options.Find(delegate (IDHCPOption v) { return v.OptionType == optionType; });
        }

        public bool IsRequestedParameter(TDHCPOption optionType)
        {
            DHCPOptionParameterRequestList dhcpOptionParameterRequestList = (DHCPOptionParameterRequestList)GetOption(TDHCPOption.ParameterRequestList);
            return (dhcpOptionParameterRequestList != null && dhcpOptionParameterRequestList.RequestList.Contains(optionType));
        }

        private DHCPMessage(Stream s) : this()
        {
            _opcode = (TOpcode)s.ReadByte();
            _hardwareType = (THardwareType)s.ReadByte();
            _clientHardwareAddress = new byte[s.ReadByte()];
            _hops = (byte)s.ReadByte();
            _xID = ParseHelper.ReadUInt32(s);
            _secs = ParseHelper.ReadUInt16(s);
            _broadCast = ((ParseHelper.ReadUInt16(s) & 0x8000) == 0x8000);
            _clientIPAddress = ParseHelper.ReadIPAddress(s);
            _yourIPAddress = ParseHelper.ReadIPAddress(s);
            _nextServerIPAddress = ParseHelper.ReadIPAddress(s);
            _relayAgentIPAddress = ParseHelper.ReadIPAddress(s);
            s.Read(_clientHardwareAddress, 0, _clientHardwareAddress.Length);
            for(int t = _clientHardwareAddress.Length; t < 16; t++) s.ReadByte();

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

            switch(overload)
            {
                default:
                    _serverHostName = ParseHelper.ReadZString(new MemoryStream(serverHostNameBuffer));
                    _bootFileName = ParseHelper.ReadZString(new MemoryStream(bootFileNameBuffer));
                    _options = ReadOptions(optionsBuffer, new byte[0], new byte[0]);
                    break;

                case 1:
                    _serverHostName = ParseHelper.ReadZString(new MemoryStream(serverHostNameBuffer));
                    _options = ReadOptions(optionsBuffer, bootFileNameBuffer, new byte[0]);
                    break;

                case 2:
                    _bootFileName = ParseHelper.ReadZString(new MemoryStream(bootFileNameBuffer));
                    _options = ReadOptions(optionsBuffer, serverHostNameBuffer, new byte[0]);
                    break;

                case 3:
                    _options = ReadOptions(optionsBuffer, bootFileNameBuffer, serverHostNameBuffer);
                    break;
            }
        }

        private static List<IDHCPOption> ReadOptions(byte[] buffer1, byte[] buffer2, byte[] buffer3)
        {
            var result = new List<IDHCPOption>();
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

        private static void ReadOptions(List<IDHCPOption> options, MemoryStream s, params MemoryStream[] spillovers)
        {
            while(true)
            {
                int code = s.ReadByte();
                if(code == -1 || code == 255) break;
                else if(code == 0) continue;
                else
                {
                    MemoryStream concatStream = new MemoryStream();
                    int len = s.ReadByte();
                    if(len == -1) break;
                    CopyBytes(s, concatStream, len);
                    AppendOverflow(code, s, concatStream);
                    foreach(MemoryStream spillOver in spillovers)
                    {
                        AppendOverflow(code, spillOver, concatStream);
                    }
                    concatStream.Position = 0;
                    options.Add(s_optionsTemplates[code].FromStream(concatStream));
                }
            }
        }

        private static void AppendOverflow(int code, MemoryStream source, MemoryStream target)
        {
            long initPosition = source.Position;
            try
            {
                while(true)
                {
                    int c = source.ReadByte();
                    if(c == -1 || c == 255) break;
                    else if(c == 0) continue;
                    else
                    {
                        int l = source.ReadByte();
                        if(l == -1) break;

                        if(c == code)
                        {
                            long startPosition = source.Position - 2;
                            CopyBytes(source, target, l);
                            source.Position = startPosition;
                            for(int t = 0; t < (l + 2); t++)
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

            while(true)
            {
                int code = s.ReadByte();
                if(code == -1 || code == 255) break;
                else if(code == 0) continue;
                else if(code == 52)
                {
                    if(s.ReadByte() != 1) throw new IOException("Invalid length of DHCP option 'Option Overload'");
                    result = (byte)s.ReadByte();
                }
                else
                {
                    int l = s.ReadByte();
                    if(l == -1) break;
                    s.Position += l;
                }
            }
            return result;
        }

        public static DHCPMessage FromStream(Stream s)
        {
            return new DHCPMessage(s);
        }

        public void ToStream(Stream s, int minimumPacketSize)
        {
            s.WriteByte((byte)_opcode);
            s.WriteByte((byte)_hardwareType);
            s.WriteByte((byte)_clientHardwareAddress.Length);
            s.WriteByte((byte)_hops);
            ParseHelper.WriteUInt32(s, _xID);
            ParseHelper.WriteUInt16(s, _secs);
            ParseHelper.WriteUInt16(s, _broadCast ? (ushort)0x8000 : (ushort)0x0);
            ParseHelper.WriteIPAddress(s, _clientIPAddress);
            ParseHelper.WriteIPAddress(s, _yourIPAddress);
            ParseHelper.WriteIPAddress(s, _nextServerIPAddress);
            ParseHelper.WriteIPAddress(s, _relayAgentIPAddress);
            s.Write(_clientHardwareAddress, 0, _clientHardwareAddress.Length);
            for(int t = _clientHardwareAddress.Length; t < 16; t++) s.WriteByte(0);
            ParseHelper.WriteZString(s, _serverHostName, 64);  // BOOTP legacy
            ParseHelper.WriteZString(s, _bootFileName, 128);   // BOOTP legacy
            s.Write(new byte[] { 99, 130, 83, 99 }, 0, 4);  // options magic cookie

            // write all options except RelayAgentInformation
            foreach(var option in _options.Where(x => x.OptionType != TDHCPOption.RelayAgentInformation))
            {
                var optionStream = new MemoryStream();
                option.ToStream(optionStream);
                s.WriteByte((byte)option.OptionType);
                s.WriteByte((byte)optionStream.Length);
                optionStream.Position = 0;
                CopyBytes(optionStream, s, (int)optionStream.Length);
            }

#if RELAYAGENTINFORMATION
            // RelayAgentInformation should be the last option before the end according to RFC 3046
            foreach (var option in _options.Where(x => x.OptionType == TDHCPOption.RelayAgentInformation))
            {
                var optionStream = new MemoryStream();
                option.ToStream(optionStream);
                s.WriteByte((byte)option.OptionType);
                s.WriteByte((byte)optionStream.Length);
                optionStream.Position = 0;
                CopyBytes(optionStream, s, (int)optionStream.Length);
            }
#endif
            // write end option
            s.WriteByte(255);
            s.Flush();

            while(s.Length < minimumPacketSize)
            {
                s.WriteByte(0);
            }

            s.Flush();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Opcode (op)                    : {0}\r\n", _opcode);
            sb.AppendFormat("HardwareType (htype)           : {0}\r\n", _hardwareType);
            sb.AppendFormat("Hops                           : {0}\r\n", _hops);
            sb.AppendFormat("XID                            : {0}\r\n", _xID);
            sb.AppendFormat("Secs                           : {0}\r\n", _secs);
            sb.AppendFormat("BroadCast (flags)              : {0}\r\n", _broadCast);
            sb.AppendFormat("ClientIPAddress (ciaddr)       : {0}\r\n", _clientIPAddress);
            sb.AppendFormat("YourIPAddress (yiaddr)         : {0}\r\n", _yourIPAddress);
            sb.AppendFormat("NextServerIPAddress (siaddr)   : {0}\r\n", _nextServerIPAddress);
            sb.AppendFormat("RelayAgentIPAddress (giaddr)   : {0}\r\n", _relayAgentIPAddress);
            sb.AppendFormat("ClientHardwareAddress (chaddr) : {0}\r\n", Utils.BytesToHexString(_clientHardwareAddress, "-"));
            sb.AppendFormat("ServerHostName (sname)         : {0}\r\n", _serverHostName);
            sb.AppendFormat("BootFileName (file)            : {0}\r\n", _bootFileName);

            foreach(IDHCPOption option in _options)
            {
                sb.AppendFormat("Option                         : {0}\r\n", option.ToString());
            }

            return sb.ToString();
        }
    }
}
