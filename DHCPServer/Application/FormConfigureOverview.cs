using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CodePlex.JPMikkers.DHCP;
using System.Reflection;
using System.IO;

namespace DHCPServerApp
{
    public partial class FormConfigureOverview : Form
    {
        private string m_ConfigurationPath;
        private DHCPServerConfigurationList m_ConfigurationList;

        public FormConfigureOverview(string configurationPath)
        {
            InitializeComponent();
            m_ConfigurationPath = configurationPath;
            m_ConfigurationList = DHCPServerConfigurationList.Read(m_ConfigurationPath);
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = m_ConfigurationList;
            UpdateButtonStatus();
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if ((dataGridView1.Rows[e.RowIndex].DataBoundItem != null) &&
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

            if (propertyName.Contains("."))
            {
                PropertyInfo[] arrayProperties;
                string leftPropertyName;

                leftPropertyName = propertyName.Substring(0, propertyName.IndexOf("."));
                arrayProperties = property.GetType().GetProperties();

                foreach (PropertyInfo propertyInfo in arrayProperties)
                {
                    if (propertyInfo.Name == leftPropertyName)
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

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            DHCPServerConfiguration result = EditConfiguration(-1,new DHCPServerConfiguration());
            if (result != null)
            {
                m_ConfigurationList.Add(result);
                SelectRow(m_ConfigurationList.Count - 1);
                UpdateButtonStatus();
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            List<int> rowsToRemove = new List<int>();            
            foreach(DataGridViewRow row in dataGridView1.SelectedRows)
            {
                rowsToRemove.Add(row.Index);   
            }
            rowsToRemove.Sort();
            rowsToRemove.Reverse();
            
            int currentIndex = dataGridView1.CurrentRow.Index;

            foreach (int x in rowsToRemove)
            {
                if (currentIndex == x && currentIndex>0)
                {
                    currentIndex--;
                    SelectRow(currentIndex);
                }             
                m_ConfigurationList.RemoveAt(x);
            }
            UpdateButtonStatus();
        }

        private void UpdateButtonStatus()
        {
            buttonRemove.Enabled = dataGridView1.Rows.Count > 0;
            buttonEdit.Enabled = dataGridView1.SelectedRows.Count > 0;
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStatus();
        }

        private void buttonEdit_Click(object sender, EventArgs e)
        {
            EditConfiguration(dataGridView1.CurrentRow.Index);
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            EditConfiguration(e.RowIndex);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            m_ConfigurationList.Write(m_ConfigurationPath);
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //if (dataGridView1.Columns[e.ColumnIndex] == ColumnRootPath)
            //{
            //    System.Diagnostics.Process.Start(m_ConfigurationList[e.RowIndex].RootPath);
            //}
        }

        private void SelectRow(int index)
        {
            if (index >= 0 && index < dataGridView1.Rows.Count)
            {
                dataGridView1.ClearSelection();
                DataGridViewRow rowToSelect = dataGridView1.Rows[index];
                rowToSelect.Selected = true;
                rowToSelect.Cells[0].Selected = true;
                dataGridView1.CurrentCell = rowToSelect.Cells[0];
            }
        }

        private void EditConfiguration(int index)
        {
            if (index >= 0 && index < m_ConfigurationList.Count)
            {
                DHCPServerConfiguration result = EditConfiguration(index,m_ConfigurationList[index]);
                if (result != null)
                {
                    m_ConfigurationList.Insert(index, result);
                    m_ConfigurationList.RemoveAt(index + 1);
                }
            }
        }

        private bool ConfigurationCollides(int index,DHCPServerConfiguration config)
        {
            for (int t = 0; t < m_ConfigurationList.Count; t++)
            {
                if (t != index && config.Address == m_ConfigurationList[t].Address)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("item {0} collides with {1}",t,index));
                    return true;
                }
            }
            return false;
        }

        private DHCPServerConfiguration EditConfiguration(int index,DHCPServerConfiguration input)
        {
            FormSettings f = new FormSettings();
            f.Configuration = input;

            DialogResult dialogResult = f.ShowDialog(this);

            while(dialogResult == DialogResult.OK && ConfigurationCollides(index,f.Configuration))
            {
                MessageBox.Show(string.Format("There already is a DHCP server configuration for address {0}.\r\nPlease select another address.", f.Configuration.Address), "Validation error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dialogResult = f.ShowDialog(this);
            }

            return dialogResult == DialogResult.OK ? f.Configuration : null;
        }
    }
}
