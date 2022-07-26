using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public enum TDHCPMessageType
    {
        DISCOVER = 1,
        OFFER,
        REQUEST,
        DECLINE,
        ACK,
        NAK,
        RELEASE,
        INFORM,
        Undefined
    }

    public class DHCPOptionMessageType : DHCPOptionBase
    {
        private TDHCPMessageType _messageType;

        #region IDHCPOption Members

        public TDHCPMessageType MessageType
        {
            get
            {
                return _messageType;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMessageType result = new DHCPOptionMessageType();
            if(s.Length != 1) throw new IOException("Invalid DHCP option length");
            result._messageType = (TDHCPMessageType)s.ReadByte();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteByte((byte)_messageType);
        }

        #endregion

        public DHCPOptionMessageType()
            : base(TDHCPOption.MessageType)
        {
        }

        public DHCPOptionMessageType(TDHCPMessageType messageType)
            : base(TDHCPOption.MessageType)
        {
            _messageType = messageType;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{_messageType}])";
        }
    }
}
