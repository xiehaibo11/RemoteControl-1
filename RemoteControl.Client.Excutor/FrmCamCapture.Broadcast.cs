using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RemoteControl.Client.Excutor
{
    public partial class FrmCamCapture
    {
        #region 数据广播

        private void StartBroadcastInternal()
        {
            Bitmap bmp = null;
            while (true)
            {
                try
                {
                    bmp = (Bitmap)this.Invoke(new Func<Bitmap>(() =>
                    {
                        return this.videoSourcePlayer1.GetCurrentVideoFrame();
                    }));
                    if (bmp != null)
                    {
                        Broadcast(bmp);
                        bmp.Dispose();
                    }
                    else
                    {
                        BroadcastDiagnosticFrame();
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _captureErrorMessage = "摄像头采集失败：" + ex.Message;
                    BroadcastDiagnosticFrame();
                }
                Thread.Sleep(_intervalMilliSec);
            }
        }

        private void StartTransportServerInternal()
        {
            if (_broadcastServer != null)
                return;
            try
            {
                _broadcastServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _broadcastServer.Bind(new IPEndPoint(IPAddress.Parse(_broadcastServerIP), _broadcastServerPort));
                _broadcastServer.Listen(10);
                new Thread(() =>
                {
                    Socket c = null;
                    string id = null;
                    while (true)
                    {
                        try
                        {
                            c = _broadcastServer.Accept();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            Environment.Exit(0);
                        }
                        id = c.RemoteEndPoint.ToString();
                        if (!_clients.ContainsKey(id))
                        {
                            while (!_clients.TryAdd(id, c)) ;
                        }
                    }
                }) { IsBackground = true }.Start();
            }
            catch (Exception)
            {
                Environment.Exit(0);
            }
        }

        private void StopTransportServer()
        {
            try
            {
                if (_broadcastServer != null)
                {
                    _broadcastServer.Close();
                    _broadcastServer = null;
                }
            }
            catch (Exception)
            {
            }
        }

        private void Broadcast(Bitmap bmp)
        {
            if (_broadcastServer != null)
            {
                byte[] data = null;
                DateTime captureTime = DateTime.Now;
                try
                {
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        // 改为jpeg后，小很多
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                        data = ms.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("生成图片字节数组失败，" + ex.Message);
                    return;
                }
                foreach (var pair in _clients)
                {
                    try
                    {
                        pair.Value.Send(Encode(captureTime, data));
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("您的主机中的软件中止了一个已建立的连接"))
                        {
                            Socket s = null;
                            _clients.TryRemove(pair.Key, out s);
                        }
                        Console.WriteLine("广播到" + pair.Key + "失败," + ex.Message);
                    }
                }
                data = null;
                Output(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + " 已广播");
            }
        }

        private void BroadcastDiagnosticFrame()
        {
            using (Bitmap bmp = CreateDiagnosticFrame())
            {
                Broadcast(bmp);
            }
        }

        private Bitmap CreateDiagnosticFrame()
        {
            string message = string.IsNullOrEmpty(_captureErrorMessage)
                ? "正在等待摄像头画面..."
                : _captureErrorMessage;
            Bitmap bmp = new Bitmap(640, 360);
            using (Graphics graphics = Graphics.FromImage(bmp))
            using (Brush backgroundBrush = new SolidBrush(Color.FromArgb(32, 38, 48)))
            using (Brush titleBrush = new SolidBrush(Color.White))
            using (Brush bodyBrush = new SolidBrush(Color.FromArgb(220, 228, 238)))
            using (Font titleFont = new Font("微软雅黑", 22F, FontStyle.Bold))
            using (Font bodyFont = new Font("微软雅黑", 13F, FontStyle.Regular))
            {
                graphics.FillRectangle(backgroundBrush, 0, 0, bmp.Width, bmp.Height);
                graphics.DrawString("摄像头暂无画面", titleFont, titleBrush, new PointF(36, 46));
                graphics.DrawString(message, bodyFont, bodyBrush, new RectangleF(38, 108, 560, 90));
                graphics.DrawString("请检查笔记本/台式机摄像头、驱动、系统权限或是否被其他程序占用。", bodyFont, bodyBrush, new RectangleF(38, 190, 560, 90));
            }
            return bmp;
        }

        private int NormalizeFps(int fps)
        {
            if (fps < 1)
            {
                return 1;
            }

            if (fps > 10)
            {
                return 10;
            }

            return fps;
        }

        private byte[] Encode(DateTime captureTime, byte[] captureData)
        {
            List<byte> data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(8 + captureData.Length));
            data.AddRange(BitConverter.GetBytes(captureTime.Ticks));
            data.AddRange(captureData);

            //System.IO.File.WriteAllBytes(@"d:\1.jpg", captureData);

            return data.ToArray();
        }

        #endregion
    }
}
