using GitHub.JPMikkers.DHCP;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.ServiceProcess;
using System.Windows.Forms;

namespace DHCPServerApp
{
    public partial class FormMain : Form
    {
        private readonly bool _hasAdministrativeRight;
        private readonly ServiceController _service;
        private DateTime _timeFilter;
        private readonly MACTaster _MACTaster;

        public FormMain(ServiceController service)
        {
            _service = service;
            _hasAdministrativeRight = Program.HasAdministrativeRight();
            _MACTaster = new MACTaster(Program.GetMacTastePath());
            InitializeComponent();
            UpdateServiceStatus();
            timerServiceWatcher.Enabled = true;
            SetTimeFilter(DateTime.Now);
            eventLog1.EnableRaisingEvents = true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
        }

        private void UpdateServiceStatus()
        {
            _service.Refresh();
            //System.Diagnostics.Debug.WriteLine(m_Service.Status.ToString());
            if(!Program.HasAdministrativeRight())
            {
                buttonStart.Enabled = false;
                buttonStop.Enabled = false;
                buttonConfigure.Enabled = false;
                buttonElevate.Enabled = true;
            }
            else
            {
                buttonStart.Enabled = (_service.Status == ServiceControllerStatus.Stopped);
                buttonStop.Enabled = (_service.Status == ServiceControllerStatus.Running);
                buttonConfigure.Enabled = true;
                buttonElevate.Enabled = false;
            }

            toolStripStatusLabel.Text = $"Service status: {_service.Status}";
        }

        private void StartService()
        {
            try
            {
                _service.Start();
            }
            catch(Exception)
            {
            }
            UpdateServiceStatus();
        }

        private void StopService()
        {
            try
            {
                _service.Stop();
            }
            catch(Exception)
            {
            }
            UpdateServiceStatus();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            StartService();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            StopService();
        }

        private void buttonElevate_Click(object sender, EventArgs e)
        {
            if(Program.RunElevated(""))
            {
                Close();
            }
        }

        private void timerServiceWatcher_Tick(object sender, EventArgs e)
        {
            UpdateServiceStatus();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AboutBox about = new AboutBox();
            about.ShowDialog(this);
        }

        private void buttonConfigure_Click(object sender, EventArgs e)
        {
            FormConfigureOverview f = new FormConfigureOverview(Program.GetConfigurationPath());
            if(f.ShowDialog(this) == DialogResult.OK)
            {
                UpdateServiceStatus();
                if(_hasAdministrativeRight && _service.Status == ServiceControllerStatus.Running)
                {
                    if(MessageBox.Show("The DHCP Service has to be restarted to enable the new settings.\r\n" +
                        "Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        _service.Stop();
                        _service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30.0));
                        _service.Start();
                    }
                }
            }
        }

        private void eventLog1_EntryWritten(object sender, System.Diagnostics.EntryWrittenEventArgs e)
        {
            if(InvokeRequired)
            {
                BeginInvoke(new EntryWrittenEventHandler(eventLog1_EntryWritten), sender, e);
                return;
            }
            else
            {
                AddEventLogEntry(e.Entry);
            }
        }

        private void AddEventLogEntry(EventLogEntry entry)
        {
            if(entry.TimeGenerated > _timeFilter)
            {
                string entryType;
                switch(entry.EntryType)
                {
                    case EventLogEntryType.Error:
                        entryType = "ERROR";
                        break;

                    case EventLogEntryType.Warning:
                        entryType = "WARNING";
                        break;

                    default:
                    case EventLogEntryType.Information:
                        entryType = "INFO";
                        break;
                }
                textBox1.AppendText(entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss.fff") + " : " + entryType + " : " + entry.Message + "\r\n");
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            SetTimeFilter(DateTime.Now);
        }

        private void RebuildLog()
        {
            textBox1.Visible = false;
            //textBox1.Up
            textBox1.Clear();

            foreach(EventLogEntry entry in eventLog1.Entries)
            {
                AddEventLogEntry(entry);
            }
            textBox1.Visible = true;
        }

        private void buttonShowHistory_Click(object sender, EventArgs e)
        {
            try
            {
                SetTimeFilter(_timeFilter.AddDays(-1));
            }
            catch(Exception)
            {
            }
        }

        private void buttonHistoryOneHour_Click(object sender, EventArgs e)
        {
            try
            {
                SetTimeFilter(_timeFilter.AddHours(-1));
            }
            catch(Exception)
            {
            }
        }

        private void SetTimeFilter(DateTime filter)
        {
            _timeFilter = filter;
            if(_timeFilter == DateTime.MinValue)
            {
                labelFilter.Text = "Showing all logging";
            }
            else
            {
                labelFilter.Text = $"Showing log starting at: {filter:yyyy-MM-dd HH:mm:ss.fff}";
            }
            RebuildLog();
        }

        private void buttonHistoryAll_Click(object sender, EventArgs e)
        {
            SetTimeFilter(DateTime.MinValue);
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            BindingList<DHCPClientDisplayItem> bindingList = new BindingList<DHCPClientDisplayItem>();

            try
            {
                DHCPServerConfigurationList configurationList =
                    DHCPServerConfigurationList.Read(Program.GetConfigurationPath());

                foreach(DHCPServerConfiguration configuration in configurationList)
                {
                    try
                    {
                        DHCPClientInformation clientInformation =
                            DHCPClientInformation.Read(
                                Program.GetClientInfoPath(configuration.Name, configuration.Address));

                        foreach(DHCPClient client in clientInformation.Clients)
                        {
                            bindingList.Add(new DHCPClientDisplayItem(configuration.Name, configuration.Address, client, _MACTaster.Taste(client.HardwareAddress)));
                        }
                    }
                    catch(Exception)
                    {
                    }
                }
            }
            catch(Exception)
            {
            }

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = bindingList;
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if((dataGridView1.Rows[e.RowIndex].DataBoundItem != null) &&
                (dataGridView1.Columns[e.ColumnIndex].DataPropertyName.Contains(".")))
            {
                e.Value = BindProperty(dataGridView1.Rows[e.RowIndex].DataBoundItem,
                       dataGridView1.Columns[e.ColumnIndex].DataPropertyName);
            }
            // modify row selection color to LightBlue:
            e.CellStyle.SelectionBackColor = Color.LightBlue;
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }

        private string BindProperty(object property, string propertyName)
        {
            string retValue = "";

            if(propertyName.Contains("."))
            {
                PropertyInfo[] arrayProperties;
                string leftPropertyName;

                leftPropertyName = propertyName.Substring(0, propertyName.IndexOf("."));
                arrayProperties = property.GetType().GetProperties();

                foreach(PropertyInfo propertyInfo in arrayProperties)
                {
                    if(propertyInfo.Name == leftPropertyName)
                    {
                        retValue = BindProperty(
                          propertyInfo.GetValue(property, null),
                          propertyName.Substring(propertyName.IndexOf(".") + 1));
                        break;
                    }
                }
            }
            else
            {
                Type propertyType;
                PropertyInfo propertyInfo;

                propertyType = property.GetType();
                propertyInfo = propertyType.GetProperty(propertyName);
                retValue = propertyInfo.GetValue(property, null).ToString();
            }

            return retValue;
        }
    }


    public class DHCPClientDisplayItem
    {
        private string m_ServerName;
        private string m_ServerIPAddress;
        private DHCPClient m_Client;
        private readonly string m_MacTaste;

        public string ServerName
        {
            get { return m_ServerName; }
            set { m_ServerName = value; }
        }

        public string ServerIPAddress
        {
            get { return m_ServerIPAddress; }
            set { m_ServerIPAddress = value; }
        }

        public DHCPClient Client
        {
            get { return m_Client; }
            set { m_Client = value; }
        }

        public string IdentifierAsString
        {
            get
            {
                return Utils.BytesToHexString(m_Client.Identifier, ":");
            }
        }

        public string HardwareAddressAsString
        {
            get
            {
                return Utils.BytesToHexString(m_Client.HardwareAddress, ":");
            }
        }

        public string LeaseStartTimeAsString
        {
            get
            {
                return m_Client.LeaseStartTime.ToString("yyyy-MM-dd hh:mm:ss");
            }
        }

        public string LeaseEndTimeAsString
        {
            get
            {
                return (m_Client.LeaseEndTime == DateTime.MaxValue) ? "Never" : m_Client.LeaseEndTime.ToString("yyyy-MM-dd hh:mm:ss");
            }
        }

        public string MACTaste
        {
            get
            {
                return m_MacTaste;
            }
        }

        public DHCPClientDisplayItem(string serverName, string serverAddress, DHCPClient client, string macTaste)
        {
            m_ServerName = serverName;
            m_ServerIPAddress = serverAddress;
            m_Client = client;
            m_MacTaste = macTaste;
        }
    }
}
