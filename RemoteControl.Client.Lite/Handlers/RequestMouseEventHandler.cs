using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Client.Handlers
{
    class RequestMouseEventHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            var req = reqObj as RequestMouseEvent;
            if (req == null)
                return;

            if (req.MouseOperation == eMouseOperations.MouseDown)
            {
                MouseOpeUtil.MouseDown(req.MouseButton, req.MouseLocation);
            }
            else if (req.MouseOperation == eMouseOperations.MouseUp)
            {
                MouseOpeUtil.MouseUp(req.MouseButton, req.MouseLocation);
            }
            else if (req.MouseOperation == eMouseOperations.MousePress)
            {
                MouseOpeUtil.MousePress(req.MouseButton, req.MouseLocation);
            }
            else if (req.MouseOperation == eMouseOperations.MouseDoubleClick)
            {
                MouseOpeUtil.MouseDoubleClick(req.MouseButton, req.MouseLocation);
            }
            else if (req.MouseOperation == eMouseOperations.MouseMove)
            {
                MouseOpeUtil.MouseMove(req.MouseLocation);
            }
        }
    }
}
