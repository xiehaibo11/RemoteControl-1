using RemoteControl.Protocals;
using RemoteControl.Protocals.Request;

namespace RemoteControl.Client.Handlers
{
    class RequestKeyboardEventHandler : AbstractRequestHandler
    {
        public override void Handle(SocketSession session, ePacketType reqType, object reqObj)
        {
            var req = reqObj as RequestKeyboardEvent;
            if (req == null)
                return;

            KeyboardOpeUtil.KeyOpe(req.KeyCode, req.KeyOperation);
        }
    }
}
