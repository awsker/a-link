using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using alink.Models;

namespace alink.UI
{
    public partial class RulesEditor : Form
    {
        private BindingList<MemoryRule> _rules;
        private RulesConfig _previousRulesConfig;
        
        public RulesEditor()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            _rules = new BindingList<MemoryRule>();
            //dataGridView1.RowsAdded += DataGridView1OnRowsAdded;
            setupColumns();
            dataGridView1.DataSource = _rules;
        }

        public RulesEditor(RulesConfig config):this()
        {
            descriptionTextBox.Text = config.Description;
            foreach(var r in config.Rules)
                _rules.Add(r.Clone());
            _previousRulesConfig = config;
        }

        #region UI Events
        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            _rules.AddNew();
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            removeSelected();
        }

        private void removeSelected()
        {
            var indexesToRemove = selectedRowIndexes().OrderByDescending(i => i).ToList();
            for (int i = indexesToRemove.Count - 1; i >= 0; --i)
            {
                _rules.RemoveAt(indexesToRemove[i]);
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            updateButtons();
        }


        private void DataGridView1OnRowsAdded(object sender, DataGridViewRowsAddedEventArgs dataGridViewRowsAddedEventArgs)
        {
            dataGridView1.ClearSelection();
            var newIndex = _rules.Count - 1;
            dataGridView1.CurrentCell = dataGridView1.Rows[newIndex].Cells[0];
            dataGridView1.BeginEdit(false);
            updateButtons();
        }
        #endregion

        #region Private methods
        private void setupColumns()
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Description",
                DataPropertyName = "Description",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Memory Offset",
                DataPropertyName = "MemoryOffset"
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "# bytes",
                DataPropertyName = "NumBytes",
                Width = 70
            });
            dataGridView1.Columns.Add(new DataGridViewComboBoxColumn()
            {
                DataSource = Enum.GetValues(typeof(DataType)),
                ValueType = typeof(DataType),
                HeaderText = "Data type",
                DataPropertyName = "DataType"
            });
            dataGridView1.Columns.Add(new DataGridViewComboBoxColumn()
            {
                DataSource = Enum.GetValues(typeof(ChangeTrigger)),
                ValueType = typeof(ChangeTrigger),
                HeaderText = "When to send",
                DataPropertyName = "ChangeTrigger"
            });
            dataGridView1.Columns.Add(new DataGridViewComboBoxColumn()
            {
                DataSource = Enum.GetValues(typeof(TransferType)),
                ValueType = typeof(TransferType),
                HeaderText = "What to send",
                DataPropertyName = "TransferType"
            });
            dataGridView1.Columns.Add(new DataGridViewComboBoxColumn()
            {
                DataSource = Enum.GetValues(typeof(Endianness)),
                ValueType = typeof(Endianness),
                HeaderText = "Endianness",
                DataPropertyName = "Endianness"
            });
            dataGridView1.Columns.Add(new DataGridViewCheckBoxColumn()
            {
                HeaderText = "Log",
                DataPropertyName = "Log",
                Width = 35
            });
        }

        private void updateButtons()
        {
            var selectCount = selectedRowIndexes().Count;
            var anySelected = selectCount > 0;
            var oneRowSelected = selectCount == 1;
            moveUpButton.Enabled = oneRowSelected;
            moveDownButton.Enabled = oneRowSelected;
            removeButton.Enabled = anySelected;
        }

        private ISet<int> selectedRowIndexes()
        {
            var set = new HashSet<int>();
            foreach (DataGridViewCell cell in dataGridView1.SelectedCells)
                set.Add(cell.RowIndex);
            return set;
        }
        #endregion

        #region Public properties

        public RulesConfig RulesConfig
        {
            get
            {
                if (_previousRulesConfig != null)
                {
                    var prevDesc = _previousRulesConfig.Description;
                    //Reset the file name if the description has changed
                    _previousRulesConfig.Description = descriptionTextBox.Text;
                    if (prevDesc != _previousRulesConfig.Description)
                        _previousRulesConfig.Filename = null;
                    lock (_previousRulesConfig)
                    {
                        _previousRulesConfig.Rules.Clear();
                        foreach (var r in _rules)
                            _previousRulesConfig.Rules.Add(r);
                    }
                    return _previousRulesConfig;
                }
                else
                {
                    return new RulesConfig(descriptionTextBox.Text, _rules);
                }
            }
        }
        #endregion

        private void moveUpButton_Click(object sender, EventArgs e)
        {
            var selectedCellIndexes =
                dataGridView1.SelectedCells.Cast<DataGridViewCell>()
                    .Select(c => new pos {x = c.ColumnIndex, y = c.RowIndex}).ToList();

            var indexes = selectedRowIndexes();
            if (indexes.Count == 1)
            {
                var index = indexes.First();
                if (index > 0)
                {
                    var rule = _rules[index];
                    _rules.RemoveAt(index);
                    _rules.Insert(index - 1, rule);
                    dataGridView1.ClearSelection();
                    foreach (pos pos in selectedCellIndexes)
                    {
                        dataGridView1.Rows[pos.y - 1].Cells[pos.x].Selected = true;
                    }
                }
            }
        }
        
        private void moveDownButton_Click(object sender, EventArgs e)
        {
            var selectedCellIndexes =
                dataGridView1.SelectedCells.Cast<DataGridViewCell>()
                    .Select(c => new pos { x = c.ColumnIndex, y = c.RowIndex }).ToList();

            var indexes = selectedRowIndexes();
            if (indexes.Count == 1)
            {
                var index = indexes.First();
                if (index < _rules.Count - 1)
                {
                    var rule = _rules[index];
                    _rules.RemoveAt(index);
                    _rules.Insert(index + 1, rule);
                    dataGridView1.ClearSelection();
                    foreach (pos pos in selectedCellIndexes)
                    {
                        dataGridView1.Rows[pos.y + 1].Cells[pos.x].Selected = true;
                    }
                }
            }
        }


        private struct pos
        {
            public int x;
            public int y;
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            var indexes = selectedRowIndexes();
            cloneRuleMenuItem.Enabled = indexes.Count == 1;
        }

        private void cloneRuleMenuItem_Click(object sender, EventArgs e)
        {
            var indexes = selectedRowIndexes();
            int accIndex = 0;
            foreach (var i in indexes)
            {
                var rule = _rules[i].Clone();
                //This is probably what the user wants
                rule.MemoryOffset64 += rule.NumBytes;
                _rules.Insert(i + ++accIndex, rule);
            }
        }

        private void removeRuleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            removeSelected();
        }
    }
}
