using System.Windows.Forms;

namespace alink.UI
{
    public partial class JoinInfoForm : Form
    {
        public JoinInfoForm()
        {
            InitializeComponent();
        }

        private bool validate()
        {
            int temp;
            bool success = true;
            if (string.IsNullOrWhiteSpace(Address))
            {
                errorProvider1.SetError(addressTextBox, "Invalid address");
                success = false;
            }
            else
            {
                errorProvider1.SetError(addressTextBox, null);
            }
            if (!int.TryParse(portTextBox.Text, out temp) || temp <= 1024 || temp > 65535)
            {
                errorProvider1.SetError(portTextBox, "Invalid port");
                success = false;
            }
            else
            {
                errorProvider1.SetError(portTextBox, null);
            }
            return success;
        }

        public string Address
        {
            get { return addressTextBox.Text; }
            set { addressTextBox.Text = value; }
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
