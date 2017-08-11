using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace alink.UI
{
    public partial class ProcessSelector : UserControl
    {
        public ProcessSelector()
        {
            InitializeComponent();
        }

        private void ProcessSelector_Load(object sender, EventArgs e)
        {
            loadProcessesIntoCombo();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            loadProcessesIntoCombo();
        }

        private void loadProcessesIntoCombo()
        {
            var processesWithWindows = Process.GetProcesses().Where(p => p.MainWindowHandle != IntPtr.Zero).OrderBy(p => p.ProcessName).ToList();

            var list = new BindingList<Process>(processesWithWindows);

            comboBox.DisplayMember = "ProcessName";

            comboBox.DataSource = list;
        }

        public Process SelectedProcess
        {
            get { return comboBox.SelectedItem as Process; }
        }


    }
}
