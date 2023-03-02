using System;

namespace Artemis.MediaInfo.MonitorState;

public class Power
{
    internal static int RegisterPowerSettingNotification(
        IntPtr handle, Guid powerSetting)
    {
        int outHandle = PowerManagementNativeMethods.RegisterPowerSettingNotification(
            handle,
            ref powerSetting,
            0);

        return outHandle;
    }   
}