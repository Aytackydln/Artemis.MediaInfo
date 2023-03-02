using System;

namespace Artemis.MediaInfo.MonitorState;

public class PowerManager
{
    
    private static bool? isMonitorOn;
    private static readonly object monitoronlock = new object();
    
    public static event EventHandler IsMonitorOnChanged
    {
        add
        {
            MessageManager.RegisterPowerEvent(
                EventManager.MonitorPowerStatus, value);
        }
        remove
        {
            MessageManager.UnregisterPowerEvent(
                EventManager.MonitorPowerStatus, value);
        }
    }
    
    public static bool IsMonitorOn
    {
        get
        {
            lock (monitoronlock)
            {
                if (isMonitorOn == null)
                {
                    EventHandler dummy = delegate(object sender, EventArgs args) { };
                    IsMonitorOnChanged += dummy;
                    // Wait until Windows updates the power source 
                    // (through RegisterPowerSettingNotification)
                    EventManager.monitorOnReset.WaitOne();
                }
            }

            return (bool)isMonitorOn;
        }
        internal set { isMonitorOn = value; }
    }
}