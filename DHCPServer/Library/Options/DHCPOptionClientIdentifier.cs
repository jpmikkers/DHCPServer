
using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionClientIdentifier : DHCPOptionBase
    {
        private DHCPMessage.THardwareType _hardwareType;
        private byte[] _data;

        public DHCPMessage.THardwareType HardwareType
        {
            get { return _hardwareType; }
            set { _hardwareType = value; }   
        }

        public byte[] Data
        {
            get { return _data; }
            set { _data = value; }
        }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionClientIdentifier result = new DHCPOptionClientIdentifier();
            _hardwareType = (DHCPMessage.THardwareType)ParseHelper.ReadUInt8(s);
            result._data = new byte[s.Length - s.Position];
            s.Read(result._data, 0, result._data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteUInt8(s, (byte)_hardwareType);
            s.Write(_data, 0, _data.Length);
        }

        #endregion

        public DHCPOptionClientIdentifier()
            : base(TDHCPOption.ClientIdentifier)
        {
            _hardwareType = DHCPMessage.THardwareType.Unknown;
            _data = new byte[0];
        }

        public DHCPOptionClientIdentifier(DHCPMessage.THardwareType hardwareType,byte[] data)
            : base(TDHCPOption.ClientIdentifier)
        {
            _hardwareType = hardwareType;
            _data = data;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],htype=[{_hardwareType}],value=[{Utils.BytesToHexString(_data, " ")}])";
        }
    }
}
