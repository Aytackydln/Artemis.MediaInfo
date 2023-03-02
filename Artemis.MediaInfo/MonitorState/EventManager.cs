using System;
using System.Threading;

namespace Artemis.MediaInfo.MonitorState;

public class EventManager
{
    internal static AutoResetEvent monitorOnReset = new(false);
    internal static readonly Guid MonitorPowerStatus = new(0x02731015, 0x4510, 0x4526, 0x99, 0xe6,
        0xe5, 0xa1, 0x7e, 0xbd, 0x1a, 0xea);
    
    private static bool monitorOnCaught;
    
    internal static bool IsMessageCaught(Guid eventGuid)
    {
        bool isMessageCaught = false;

        if (eventGuid == MonitorPowerStatus)
        {
            if (!monitorOnCaught)
            {
                monitorOnCaught = true;
                isMessageCaught = true;
            }
        }

        return isMessageCaught;
    }
}