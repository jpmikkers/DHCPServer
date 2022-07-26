using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net.Configuration;
using System.Reflection;

namespace DHCPServerApp
{
    public partial class FormPickAdapter : Form
    {
        private IPAddress _address = IPAddress.Loopback;

        public IPAddress Address
        {
            get { return _address; }
        }

        public FormPickAdapter()
        {
            InitializeComponent();
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            comboBoxAdapter.DisplayMember = "Description";
            foreach (NetworkInterface adapter in nics)
            {
                comboBoxAdapter.Items.Add(adapter);
            }
            comboBoxAdapter.SelectedIndex = 0;
        }

        private void comboBoxAdapter_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxAdapter.SelectedIndex >= 0 && comboBoxAdapter.SelectedIndex< comboBoxAdapter.Items.Count)
            {
                comboBoxUnicast.SelectedIndex = -1;
                comboBoxUnicast.Items.Clear();

                try
                {
                    NetworkInterface adapter = (NetworkInterface)comboBoxAdapter.SelectedItem;

                    foreach (var uni in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            comboBoxUnicast.Items.Add(uni.Address);
                        }
                    }

                    foreach (var uni in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (uni.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            comboBoxUnicast.Items.Add(uni.Address);
                        }
                    }
                }
                catch (Exception)
                {
                }

                if (comboBoxUnicast.Items.Count > 0)
                {
                    comboBoxUnicast.SelectedIndex = 0;
                }
            }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            if (comboBoxUnicast.SelectedIndex >= 0)
            {
                _address = (IPAddress)comboBoxUnicast.SelectedItem;
            }
        }
    }
}
