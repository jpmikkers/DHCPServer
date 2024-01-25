using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionMessage : DHCPOptionBase
    {
        private string _message = string.Empty;

        #region IDHCPOption Members

        public string Message
        {
            get
            {
                return _message;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMessage result = new DHCPOptionMessage();
            result._message = ParseHelper.ReadString(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteString(s, ZeroTerminatedStrings, _message);
        }

        #endregion

        public DHCPOptionMessage()
            : base(TDHCPOption.Message)
        {
        }

        public DHCPOptionMessage(string message)
            : base(TDHCPOption.Message)
        {
            _message = message;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{_message}])";
        }
    }
}
