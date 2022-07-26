using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionFullyQualifiedDomainName : DHCPOptionBase
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
            DHCPOptionFullyQualifiedDomainName result = new DHCPOptionFullyQualifiedDomainName();
            result._data = new byte[s.Length];
            s.Read(result._data, 0, result._data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(_data, 0, _data.Length);
        }

        #endregion

        public DHCPOptionFullyQualifiedDomainName()
            : base(TDHCPOption.FullyQualifiedDomainName)
        {
            _data = new byte[0];
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{Utils.BytesToHexString(_data, " ")}])";
        }
    }
}
