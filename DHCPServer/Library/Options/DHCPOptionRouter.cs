using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionRouter : DHCPOptionServerListBase
    {
        public override DHCPOptionServerListBase Create()
        {
            return new DHCPOptionRouter();
        }

        public DHCPOptionRouter() : base(TDHCPOption.Router)
        {
        }
    }
}
