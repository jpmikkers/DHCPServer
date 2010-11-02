/*

Copyright (c) 2010 Jean-Paul Mikkers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/
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
        private IPAddress m_Address = IPAddress.Loopback;

        public IPAddress Address
        {
            get { return m_Address; }
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

                    foreach (UnicastIPAddressInformation uni in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            comboBoxUnicast.Items.Add(uni.Address);
                        }
                    }

                    foreach (UnicastIPAddressInformation uni in adapter.GetIPProperties().UnicastAddresses)
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
                m_Address = (IPAddress)comboBoxUnicast.SelectedItem;
            }
        }
    }
}
