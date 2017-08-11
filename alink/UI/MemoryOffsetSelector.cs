using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using alink.Models;
using alink.Utils;

namespace alink.UI
{
    public partial class MemoryOffsetSelector : UserControl
    {
        private BindingList<MemoryOffset> _list;

        public MemoryOffsetSelector()
        {
            InitializeComponent();
        }

        #region Public properties
        public MemoryOffset SelectedOffset
        {
            get { return comboBox1.SelectedItem as MemoryOffset; }
        }

        public IEnumerable<MemoryOffset> MemoryOffsets
        {
            get { return _list.Skip(1); }
        }
        #endregion

        #region UIEvents
        private void MemoryOffsetSelector_Load(object sender, EventArgs e)
        {
            loadMemoryOffsetCombo();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateButtons();
        }
        
        private void addButton_Click(object sender, EventArgs e)
        {
            var dialog = new MemoryOffsetEditor();
            var res = dialog.ShowDialog(ParentForm);
            if (res == DialogResult.OK)
            {
                _list.Add(dialog.MemoryOffset);
                saveMemoryOffsetsToFile();
                comboBox1.SelectedIndex = _list.Count - 1;
            }
        }

        private void editButton_Click(object sender, EventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            if (index > 0)
            {
                var dialog = new MemoryOffsetEditor(SelectedOffset);
                var res = dialog.ShowDialog(ParentForm);
                if (res == DialogResult.OK)
                {
                    _list.RemoveAt(index);
                    _list.Insert(index, dialog.MemoryOffset);
                    saveMemoryOffsetsToFile();
                    comboBox1.SelectedIndex = _list.Count - 1;
                    updateButtons();
                }
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            int index = comboBox1.SelectedIndex;
            if (index > 0)
            {
                _list.RemoveAt(index);
            }
        }
        #endregion

        #region Private methods
        private void updateButtons()
        {
            var anySelected = SelectedOffset != null;
            deleteButton.Enabled = anySelected;
            editButton.Enabled = anySelected;
        }

        private void saveMemoryOffsetsToFile()
        {
            IOManager.WriteMemoryOffsetsToFile(MemoryOffsets);
        }

        private void loadMemoryOffsetCombo()
        {
            _list = new BindingList<MemoryOffset> { new MemoryOffset("-", 0, OffsetType.IntPointer) };
            foreach (var mo in IOManager.ReadMemoryOffsetsFromFile())
            {
                _list.Add(mo);
            }
            comboBox1.DataSource = _list;
        }
        #endregion


    }
}
