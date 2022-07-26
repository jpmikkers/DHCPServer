using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionMaximumDHCPMessageSize : DHCPOptionBase
    {
        private ushort _maxSize;

        #region IDHCPOption Members

        public ushort MaxSize
        {
            get
            {
                return _maxSize;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMaximumDHCPMessageSize result = new DHCPOptionMaximumDHCPMessageSize();
            if (s.Length != 2) throw new IOException("Invalid DHCP option length");
            result._maxSize = ParseHelper.ReadUInt16(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteUInt16(s,_maxSize);
        }

        #endregion

        public DHCPOptionMaximumDHCPMessageSize()
            : base(TDHCPOption.MaximumDHCPMessageSize)
        {
        }

        public DHCPOptionMaximumDHCPMessageSize(ushort maxSize)
            : base(TDHCPOption.MaximumDHCPMessageSize)
        {
            _maxSize = maxSize;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{_maxSize}])";
        }
    }
}
