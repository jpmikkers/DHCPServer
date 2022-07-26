using System.IO;

namespace GitHub.JPMikkers.DHCP
{
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
            return $"Option(name=[{_optionType}],value=[])";
        }
    }
}
