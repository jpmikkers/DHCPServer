
using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionBootFileName : DHCPOptionBase
    {
        private string _name = string.Empty;

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
            DHCPOptionBootFileName result = new DHCPOptionBootFileName();
            result._name = ParseHelper.ReadString(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, ZeroTerminatedStrings, _name);
        }

        #endregion

        public DHCPOptionBootFileName()
            : base(TDHCPOption.BootFileName)
        {
        }

        public DHCPOptionBootFileName(string name)
            : base(TDHCPOption.BootFileName)
        {
            _name = name;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{_name}])";
        }
    }
}
