using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionGeneric : DHCPOptionBase
    {
        private byte[] _data;

        public byte[] Data
        {
            get { return _data; }
            set { _data = value; }
        }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionGeneric result = new DHCPOptionGeneric(_optionType);
            result._data = new byte[s.Length];
            s.Read(result._data, 0, result._data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(_data, 0, _data.Length);
        }

        #endregion

        public DHCPOptionGeneric(TDHCPOption option) : base(option)
        {
            _data = new byte[0];
        }

        public DHCPOptionGeneric(TDHCPOption option, byte[] data) : base(option)
        {
            _data = data;
        }

        public override string ToString()
        {
            return $"Option(name=[{_optionType}],value=[{Utils.BytesToHexString(_data, " ")}])";
        }
    }
}
