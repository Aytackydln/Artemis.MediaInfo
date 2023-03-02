using System;
using System.Runtime.InteropServices;

namespace Artemis.MediaInfo.MonitorState;

public class PowerManagementNativeMethods
{
    internal const uint PowerBroadcastMessage = 536;
    internal const uint PowerSettingChangeMessage = 32787;
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PowerBroadcastSetting
    {
        public Guid PowerSetting;
        public Int32 DataLength;
    }
    
    [DllImport("User32", SetLastError = true,
        EntryPoint = "RegisterPowerSettingNotification",
        CallingConvention = CallingConvention.StdCall)]
    internal static extern int RegisterPowerSettingNotification(
        IntPtr hRecipient,
        ref Guid PowerSettingGuid,
        Int32 Flags);
}