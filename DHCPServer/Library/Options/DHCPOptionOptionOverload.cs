using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionOptionOverload : DHCPOptionBase
    {
        private byte _overload;

        #region IDHCPOption Members

        public byte Overload
        {
            get
            {
                return _overload;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionOptionOverload result = new DHCPOptionOptionOverload();
            if (s.Length != 1) throw new IOException("Invalid DHCP option length");
            result._overload = (byte)s.ReadByte();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteByte(_overload);
        }

        #endregion

        public DHCPOptionOptionOverload()
            : base(TDHCPOption.OptionOverload)
        {
        }

        public DHCPOptionOptionOverload(byte overload)
            : base(TDHCPOption.OptionOverload)
        {
            _overload = overload;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{_overload}])";
        }
    }
}
