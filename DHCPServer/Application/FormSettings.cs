using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using GitHub.JPMikkers.DHCP;

namespace DHCPServerApp
{
    public partial class FormSettings : Form
    {
        private DHCPServerConfiguration _configuration;

        public DHCPServerConfiguration Configuration
        {
            get
            {
                return _configuration.Clone();
            }
            set
            {
                _configuration = value.Clone();
                Bind();
            }
        }

        public FormSettings()
        {
            InitializeComponent();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
        }

        private void Bind()
        {
            textBoxName.DataBindings.Clear();
            textBoxAddress.DataBindings.Clear();
            textBoxNetMask.DataBindings.Clear();
            textBoxPoolStart.DataBindings.Clear();
            textBoxPoolEnd.DataBindings.Clear();
            textBoxLeaseTime.DataBindings.Clear();
            textBoxOfferTime.DataBindings.Clear();
            textBoxMinimumPacketSize.DataBindings.Clear();
            var bs = new BindingSource(_configuration, null);
            textBoxName.DataBindings.Add("Text", bs, "Name");
            textBoxAddress.DataBindings.Add("Text", bs, "Address");
            textBoxNetMask.DataBindings.Add("Text", bs, "NetMask");
            textBoxPoolStart.DataBindings.Add("Text", bs, "PoolStart");
            textBoxPoolEnd.DataBindings.Add("Text", bs, "PoolEnd");
            textBoxLeaseTime.DataBindings.Add("Text", bs, "LeaseTime");
            textBoxOfferTime.DataBindings.Add("Text", bs, "OfferTime");
            textBoxMinimumPacketSize.DataBindings.Add("Text", bs, "MinimumPacketSize");
        }

        private void buttonPickAddress_Click(object sender, EventArgs e)
        {
            FormPickAdapter f = new FormPickAdapter();
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                IPAddress address = f.Address;
                IPAddress netmask = Utils.GetSubnetMask(f.Address);

                _configuration.Address = address.ToString();
                _configuration.NetMask = netmask.ToString();
                _configuration.PoolStart = Utils.UInt32ToIPAddress(Utils.IPAddressToUInt32(address) & Utils.IPAddressToUInt32(netmask)).ToString();
                _configuration.PoolEnd = Utils.UInt32ToIPAddress(
                    (Utils.IPAddressToUInt32(address) & Utils.IPAddressToUInt32(netmask)) |
                    ~Utils.IPAddressToUInt32(netmask)).ToString();
                Bind();
            }
        }
    }
}
