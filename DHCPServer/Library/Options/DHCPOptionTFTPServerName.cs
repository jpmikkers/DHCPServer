using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionTFTPServerName : DHCPOptionBase
    {
        private string _name;

        #region IDHCPOption Members

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionTFTPServerName result = new DHCPOptionTFTPServerName();
            result._name = ParseHelper.ReadString(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, ZeroTerminatedStrings, _name);
        }

        #endregion

        public DHCPOptionTFTPServerName()
            : base(TDHCPOption.TFTPServerName)
        {
        }

        public DHCPOptionTFTPServerName(string name)
            : base(TDHCPOption.TFTPServerName)
        {
            _name = name;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{_name}])";
        }
    }
}
