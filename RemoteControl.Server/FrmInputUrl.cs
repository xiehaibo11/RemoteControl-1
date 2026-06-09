using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    public partial class FrmInputUrl : FrmBase
    {
        public string InputText;

        public FrmInputUrl()
        {
            InitializeComponent();
        }

        public void SetLabel(string labelText)
        {
            this.Text = labelText;
        }

        public string GetInputText()
        {
            return this.InputText ?? this.textBox1.Text;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            this.InputText = this.textBox1.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.InputText = null;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
