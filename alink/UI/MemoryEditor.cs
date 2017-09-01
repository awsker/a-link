using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using alink.Models;
using alink.Utils;
using System.Globalization;
using System.Reflection;

namespace alink.UI
{
    public partial class MemoryEditor : Form
    {
        private BindingList<RuleMemoryContainer> _rules;
        private ProcessManager _processManager;

        public MemoryEditor(ProcessManager processManager)
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            _rules = new BindingList<RuleMemoryContainer>();
            _processManager = processManager;
            _processManager.MemoryChanged += onMemoryChanged;
            setupColumns();
            
            foreach (var r in processManager.RulesConfig.Rules)
            {
                _rules.Add(buildRuleMemory(r));
            }
            dataGridView1.DataSource = _rules;
        }

        private RuleMemoryContainer buildRuleMemory(MemoryRule rule)
        {
            object data = "-";
            var dataFromRule = _processManager.GetDataFromRule(rule);
            return new RuleMemoryContainer(rule, dataFromRule ?? data, _processManager.BaseMemoryOffset + rule.MemoryOffset64);
        }


        private void onMemoryChanged(object sender, MemoryChangedEventArgs e)
        {
            var matchingRule = _rules.FirstOrDefault(r => r.MemoryOffset64 == e.MemoryRule.MemoryOffset64);
            if (matchingRule != null)
                matchingRule.Data = _processManager.GetDataFromRule(e.MemoryRule);
        }

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
                HeaderText = "Actual Address",
                DataPropertyName = "MemPos"
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "# bytes",
                DataPropertyName = "NumBytes",
                Width = 70
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Data type",
                DataPropertyName = "DataType"
            });
            dataGridView1.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Data",
                DataPropertyName = "Data"
            });
        }
        
        private void closeButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        
        private void MemoryEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            _processManager.MemoryChanged -= onMemoryChanged;
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if(e.ColumnIndex == 5)
            {
                var rule = _rules[e.RowIndex];
                if (rule.Data == null)
                    return;
                var newValueString = rule.Data.ToString();
                var newBytes = bytesFromStringForRule(rule.MemoryRule, newValueString);
                if (newBytes != null)
                {
                    var ruleIndex = _processManager.FindRuleIndex(rule.MemoryOffset64);
                    if(ruleIndex > -1)
                        _processManager.InjectDataFromExternal(ruleIndex, rule.MemoryOffset64, newBytes, rule.MemoryOffset64, null);
                }
            }
        }

        private byte[] bytesFromStringForRule(MemoryRule rule, string valuestring)
        {
            if (rule.DataType == DataType.Data)
            {
                //Can only convert a string to a single byte value
                if (rule.NumBytes == 1)
                {
                    byte b;
                    if(byte.TryParse(valuestring, out b))
                        return new byte[] {b};
                }
            }
            else if (rule.DataType == DataType.Decimal)
            {
                if (rule.NumBytes == 4)
                {
                    float f;
                    if (float.TryParse(valuestring.Replace(",", "."), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out f))
                        return BitConverter.GetBytes(f);
                }
                else if (rule.NumBytes == 8)
                {
                    double d;
                    if (double.TryParse(valuestring.Replace(",", "."), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
                        return BitConverter.GetBytes(d);
                }
            }
            else if (rule.DataType == DataType.SignedInteger)
            {
                if (rule.NumBytes == 1)
                {
                    sbyte b;
                    if (sbyte.TryParse(valuestring, out b))
                        return new byte[] { (byte)b };
                }
                else if (rule.NumBytes == 2)
                {
                    short s;
                    if (short.TryParse(valuestring, out s))
                        return BitConverter.GetBytes(s);
                }
                else if (rule.NumBytes == 4)
                {
                    int i;
                    if (int.TryParse(valuestring, out i))
                        return BitConverter.GetBytes(i);
                }
                else if (rule.NumBytes == 8)
                {
                    long l;
                    if (long.TryParse(valuestring, out l))
                        return BitConverter.GetBytes(l);
                }
            }
            else if (rule.DataType == DataType.UnsignedInteger)
            {
                if (rule.NumBytes == 1)
                {
                    byte b;
                    if (byte.TryParse(valuestring, out b))
                        return new byte[] { b };
                }
                else if (rule.NumBytes == 2)
                {
                    ushort s;
                    if (ushort.TryParse(valuestring, out s))
                        return BitConverter.GetBytes(s);
                }
                else if (rule.NumBytes == 4)
                {
                    uint i;
                    if (uint.TryParse(valuestring, out i))
                        return BitConverter.GetBytes(i);
                }
                else if (rule.NumBytes == 8)
                {
                    ulong l;
                    if (ulong.TryParse(valuestring, out l))
                        return BitConverter.GetBytes(l);
                }
            }
            return null;
        }


        private class RuleMemoryContainer : INotifyPropertyChanged
        {
            public MemoryRule MemoryRule { get; private set; }

            public string Description
            {
                get { return MemoryRule.Description; }
                set
                {
                    if (MemoryRule.Description != value)
                    {
                        MemoryRule.Description = value;
                        onPropertyChanged();
                    }
                }
            }

            public string MemoryOffset
            {
                get { return MemoryRule.MemoryOffset; }
                set
                {
                    var newVal = Convert.ToInt64(value, 16);
                    if (MemoryRule.MemoryOffset64 != newVal)
                    {
                        MemoryRule.MemoryOffset64 = newVal;
                        onPropertyChanged();
                    }
                }
            }

            public long MemoryOffset64 { get; private set; }
            public string MemPos { get; private set; }
            public int NumBytes { get; private set; }
            public DataType DataType { get; private set; }

           private object _data;

            public object Data
            {
                get
                {
                    return _data;
                }
                set
                {
                    if (_data != value)
                    {
                        _data = value;
                        onPropertyChanged();
                    }
                }
            }

            public RuleMemoryContainer(MemoryRule rule, object data, long actualMemoryPos)
            {
                MemoryRule = rule;
                Description = rule.Description;
                MemoryOffset64 = rule.MemoryOffset64;
                MemPos = actualMemoryPos.ToString("X");
                NumBytes = rule.NumBytes;
                DataType = rule.DataType;
                Data = data;
            }

            private void onPropertyChanged([CallerMemberName] string propName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

    }
}
