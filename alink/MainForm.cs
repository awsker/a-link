using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using alink.UI;
using alink.Utils;

namespace alink
{
    public partial class MainForm : Form
    {
        private ProcessManager _processManager;
        public MainForm()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            initEvents();
            var ns = IOManager.ReadNetSettings();
            if (ns != null)
                lobbyContainer1.NetSettings = ns;
        }

        private void initEvents()
        {
            rulesSelector1.SelectedRulesConfigChanged += OnSelectedRulesConfigChanged;
            rulesSelector1.RequestMemoryEdit += RulesSelectorOnRequestMemoryEdit;
        }

        private void OnMemoryOffsetChanged(object sender, EventArgs e)
        {
            IOManager.WriteMemoryOffsetsToFile(memoryOffsetSelector.MemoryOffsets);
        }

        private void OnSelectedRulesConfigChanged(object sender, EventArgs eventArgs)
        {
            _processManager?.SetRulesConfig(rulesSelector1.SelectedConfig);
        }

        private void RulesSelectorOnRequestMemoryEdit(object sender, EventArgs eventArgs)
        {
            if (_processManager != null)
            {
                var memoryEditor = new MemoryEditor(_processManager);
                memoryEditor.ShowDialog(this);
                IOManager.WriteRulesConfig(rulesSelector1.SelectedConfig);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            saveData();
        }

        private void saveData()
        {
            IOManager.WriteMemoryOffsetsToFile(memoryOffsetSelector.MemoryOffsets);
            IOManager.WriteNetSettings(lobbyContainer1.NetSettings);
        }

        private void attachButton_Click(object sender, System.EventArgs e)
        {
            try
            {
                string errorMsg;
                if (!canAttachChecker(out errorMsg))
                {
                    MessageBox.Show(errorMsg, "Can not attach", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                if (_processManager == null)
                    attachManager();
                else
                    detachManager();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.GetType().Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusTextError(ex.Message);
                reset();
            }
        }

        private bool canAttachChecker(out string message)
        {
            if (rulesSelector1.SelectedConfig == null)
            {
                message = "No rules configuration selected";
                return false;
            }
            message = null;
            return true;
        }

        private void attachManager()
        {
            if (_processManager != null)
                return;
            attachButton.Enabled = false;
            processSelector1.Enabled = false;
            memoryOffsetSelector.Enabled = false;

            _processManager = new ProcessManager(processSelector1.SelectedProcess, memoryOffsetSelector.SelectedOffset, rulesSelector1.SelectedConfig, 32);

            lobbyContainer1.ProcessManager = _processManager;

            addEventListeners();
            _processManager.Start();
            
        }

        private void detachManager()
        {
            if (_processManager == null)
                return;

            attachButton.Enabled = false;

            _processManager.Stop();
        }

        private void ProcessManagerOnProcessStarted(object sender, EventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { ProcessManagerOnProcessStarted(sender, eventArgs); }));
                return;
            }
            statusTextSuccess("Attached to process " + processSelector1.SelectedProcess.ProcessName);
            attachButton.Text = "Detach";
            attachButton.Enabled = true;
        }

        private void ProcessManagerOnProcessStopped(object sender, EventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => { ProcessManagerOnProcessStopped(sender, eventArgs); }));
                return;
            }
            statusTextSuccess("Detatched from process");
            reset();
        }

        private void statusTextSuccess(string text)
        {
            toolStripLabel.Text = text;
            toolStripLabel.ForeColor = Color.Green;
        }

        private void statusTextError(string text)
        {
            toolStripLabel.Text = text;
            toolStripLabel.ForeColor = Color.DarkRed;
        }

        private void statusTextClear()
        {
            toolStripLabel.Text = string.Empty;
            toolStripLabel.ForeColor = Color.Black;
        }

        private void reset()
        {
            if(_processManager != null && _processManager.Running)
                _processManager.Stop();
            
            attachButton.Text = "Attach!";
            
            attachButton.Enabled = true;
            processSelector1.Enabled = true;
            memoryOffsetSelector.Enabled = true;
            removeEventListeners();

            _processManager = null;
        }

        private void addEventListeners()
        {
            if (_processManager == null)
                return;
            _processManager.ProcessStarted += ProcessManagerOnProcessStarted;
            _processManager.ProcessStopped += ProcessManagerOnProcessStopped;
        }

        private void removeEventListeners()
        {
            if (_processManager == null)
                return;
            _processManager.ProcessStarted -= ProcessManagerOnProcessStarted;
            _processManager.ProcessStopped -= ProcessManagerOnProcessStopped;
        }
    }
}
