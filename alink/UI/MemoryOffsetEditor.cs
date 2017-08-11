using System;
using System.Windows.Forms;
using alink.Models;

namespace alink.UI
{
    public partial class MemoryOffsetEditor : Form
    {
        public MemoryOffsetEditor()
        {
            InitializeComponent();
            intPointerRadioButton.Checked = true;
        }

        public MemoryOffsetEditor(MemoryOffset offset):this()
        {
            descriptionTextBox.Text = offset.Description;
            addressTextBox.Text = offset.MemoryOffsetAddress.ToString("X");
            if (offset.OffsetType == OffsetType.IntPointer)
                intPointerRadioButton.Checked = true;
            else if (offset.OffsetType == OffsetType.LongPointer)
                longPointerRadioButton.Checked = true;
            else
                absoluteOffsetRadioButton.Checked = true;
        }

        public MemoryOffset MemoryOffset
        {
            get
            {
                return new MemoryOffset(descriptionTextBox.Text, addressTextBox.Text, OffsetType);
            }
        }

        private OffsetType OffsetType
        {
            get
            {
                if(intPointerRadioButton.Checked)
                    return OffsetType.IntPointer;
                if(longPointerRadioButton.Checked)
                    return OffsetType.LongPointer;
                else
                    return OffsetType.AbsoluteOffset;
            }
        }

        private void cancelButton_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void okButton_Click(object sender, System.EventArgs e)
        {
            if (!validateInput())
            {
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool validateInput()
        {
            try
            {
                Convert.ToInt64(addressTextBox.Text, 16);
            }
            catch
            {
                errorProvider1.SetError(addressTextBox, "Invalid address. Expected hexadecimal");

                return false;
            }
            errorProvider1.SetError(addressTextBox, null);
            return true;
        }
    }
}
