using System.Collections.Generic;
using System.Management;
using System.Security.Principal;
using Artemis.Core.Modules;
using Artemis.MediaInfo.DataModels;
using Microsoft.Win32;

namespace Artemis.MediaInfo;

public class WindowsInfoModule: Module<WindowsInfoDataModel>
{
    private const string BlueLightReductionStateKey = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CloudStore\\" + 
                                 @"Store\\DefaultAccount\\Current\\default$windows.data.bluelightreduction.bluelightreductionstate\\" + 
                                 @"windows.data.bluelightreduction.bluelightreductionstate";

    private ManagementEventWatcher _nightLightStateWatcher;

    public override void Enable()
    {
        StartListenNightLight();
        UpdateNightLight();
        StartListenLockState();
    }

    public override void Disable()
    {
        _nightLightStateWatcher.EventArrived -= _nightLightStateWatcher_Changed;
        _nightLightStateWatcher.Stop();
        _nightLightStateWatcher.Dispose();
        _nightLightStateWatcher = null;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
    }

    public override void Update(double deltaTime)
    {
    }

    public override List<IModuleActivationRequirement> ActivationRequirements { get; } = new();

    private void StartListenLockState()
    {
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        DataModel.SessionLocked = e.Reason switch
        {
            SessionSwitchReason.SessionLock => true,
            SessionSwitchReason.SessionUnlock => false,
            _ => DataModel.SessionLocked
        };
    }

    private void StartListenNightLight()
    {
        var currentUser = WindowsIdentity.GetCurrent();
        ManagementScope scope = new ManagementScope("\\\\.\\root\\default");
        var quertString = string.Format(
            "SELECT * FROM RegistryValueChangeEvent WHERE Hive='HKEY_USERS' AND KeyPath='{0}\\\\{1}' AND ValueName='{2}'",
            currentUser.User.Value, BlueLightReductionStateKey.Replace("\\", "\\\\"), "Data");
        var query = new WqlEventQuery(quertString);
        _nightLightStateWatcher = new ManagementEventWatcher(scope, query);
        _nightLightStateWatcher.EventArrived += _nightLightStateWatcher_Changed;
        _nightLightStateWatcher.Start();
    }

    private void _nightLightStateWatcher_Changed(object sender, EventArrivedEventArgs e)
    {
        UpdateNightLight();
    }

    private void UpdateNightLight()
    {
        using var key = Registry.CurrentUser.OpenSubKey(BlueLightReductionStateKey);
        var data = key?.GetValue("Data");
        if (data is null)
        {
            DataModel.NightLightsEnabled = false;
            return;
        }

        var byteData = (byte[]) data;
        DataModel.NightLightsEnabled = byteData.Length > 24 && byteData[23] == 0x10 && byteData[24] == 0x00;
    }
}