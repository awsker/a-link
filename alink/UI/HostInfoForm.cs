using System.Windows.Forms;

namespace alink.UI
{
    public partial class HostInfoForm : Form
    {
        public HostInfoForm()
        {
            InitializeComponent();
        }

        private bool validate()
        {
            int temp;
            if (!int.TryParse(portTextBox.Text, out temp)) return false;
            return temp > 1024 && temp <= 65535;
        }

        public int Port
        {
            get { return int.Parse(portTextBox.Text); }
            set { portTextBox.Text = value.ToString(); }
        }

        private void okButton_Click(object sender, System.EventArgs e)
        {
            if (!validate())
            {
                errorProvider1.SetError(portTextBox, "Invalid port");
                return;
            }
            errorProvider1.SetError(portTextBox, null);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
