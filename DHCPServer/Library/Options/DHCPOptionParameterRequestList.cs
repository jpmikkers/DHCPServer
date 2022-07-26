using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionParameterRequestList : DHCPOptionBase
    {
        private readonly List<TDHCPOption> _requestList = new List<TDHCPOption>();

        #region IDHCPOption Members

        public List<TDHCPOption> RequestList
        {
            get
            {
                return _requestList;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionParameterRequestList result = new DHCPOptionParameterRequestList();
            while(true)
            {
                int c = s.ReadByte();
                if(c < 0) break;
                result._requestList.Add((TDHCPOption)c);
            }
            return result;
        }

        public override void ToStream(Stream s)
        {
            foreach(TDHCPOption opt in _requestList)
            {
                s.WriteByte((byte)opt);
            }
        }

        #endregion

        public DHCPOptionParameterRequestList()
            : base(TDHCPOption.ParameterRequestList)
        {
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(TDHCPOption opt in _requestList)
            {
                sb.Append(opt.ToString());
                sb.Append(",");
            }
            if(_requestList.Count > 0) sb.Remove(sb.Length - 1, 1);
            return $"Option(name=[{OptionType}],value=[{sb}])";
        }
    }
}
