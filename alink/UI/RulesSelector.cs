using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using alink.Models;
using alink.Utils;

namespace alink.UI
{
    public partial class RulesSelector : UserControl
    {
        private BindingList<RulesConfig> _list;

        public event EventHandler SelectedRulesConfigChanged;

        public event EventHandler RequestMemoryEdit;

        public RulesSelector()
        {
            InitializeComponent();
        }

        public RulesConfig SelectedConfig
        {
            get { return comboBox1.SelectedIndex < 0 ? null : comboBox1.SelectedItem as RulesConfig; }
        }

        private void RulesSelector_Load(object sender, EventArgs e)
        {
            loadRulesCombo();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateButtons();
            SelectedRulesConfigChanged?.Invoke(this, new EventArgs());
        }

        private void updateButtons()
        {
            var anySelected = comboBox1.SelectedIndex >= 0;
            deleteButton.Enabled = anySelected;
            editButton.Enabled = anySelected;
        }

        private void loadRulesCombo()
        {
            _list = new BindingList<RulesConfig>();
            foreach (var rc in IOManager.ReadRulesConfigsFromLocation())
            {
                _list.Add(rc);
            }
            comboBox1.DataSource = _list;
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            var dialog = new RulesEditor();
            var res = dialog.ShowDialog(this.ParentForm);
            if (res == DialogResult.OK)
            {
                var config = dialog.RulesConfig;
                _list.Add(config);
                IOManager.WriteRulesConfig(config);
                comboBox1.SelectedIndex = _list.Count - 1;
                updateButtons();
            }
        }

        private void editButton_Click(object sender, EventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            if (index >= 0)
            {
                var config = SelectedConfig;
                var filenameBeforeEdit = config.Filename;
                var descriptionBeforeEdit = config.Description;
                var dialog = new RulesEditor(config);
                var res = dialog.ShowDialog(ParentForm);
                if (res == DialogResult.OK)
                {
                    var newConfig = dialog.RulesConfig;
                    if (filenameBeforeEdit != null && descriptionBeforeEdit != null && newConfig.Description != descriptionBeforeEdit)
                    {
                        if(File.Exists(filenameBeforeEdit))
                            File.Delete(filenameBeforeEdit);

                    }
                    IOManager.WriteRulesConfig(newConfig);
                    
                    updateButtons();
                }
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            var config = SelectedConfig;
            if (config != null && comboBox1.SelectedIndex >= 0)
            {
                if (config.Filename != null)
                {
                    File.Delete(config.Filename);
                }
                _list.RemoveAt(comboBox1.SelectedIndex);
            }
        }

        private void editButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                RequestMemoryEdit?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
