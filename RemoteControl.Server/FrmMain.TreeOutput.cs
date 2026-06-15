using System;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        private void UpdateUI(Action action)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Action>(UpdateUI), action);
                return;
            }
            action();
        }

        private void doOutput(string sMsg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(doOutput), sMsg);
                return;
            }
            this.richTextBox1.Text = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + sMsg + "\r\n" + this.richTextBox1.Text;
        }
    }
}
