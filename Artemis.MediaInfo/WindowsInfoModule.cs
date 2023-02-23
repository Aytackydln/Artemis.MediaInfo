using System.Collections.Generic;
using Artemis.Core.Modules;
using Artemis.MediaInfo.DataModels;
using Artemis.MediaInfo.Utils;
using Microsoft.Win32;
using SkiaSharp;

namespace Artemis.MediaInfo;

public class WindowsInfoModule : Module<WindowsInfoDataModel>
{
    private readonly RegistryWatcher _nightLightStateWatcher = new(WatchedRegistry.CurrentUser,
        @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CloudStore\\" +
        @"Store\\DefaultAccount\\Current\\default$windows.data.bluelightreduction.bluelightreductionstate\\" +
        @"windows.data.bluelightreduction.bluelightreductionstate", "Data");
    private readonly RegistryWatcher _accentColorWatcher = new(WatchedRegistry.CurrentUser,
        @"SOFTWARE\\Microsoft\\Windows\\DWM", "AccentColor");

    public override void Enable()
    {
        _nightLightStateWatcher.RegistryChanged += UpdateNightLight;
        _nightLightStateWatcher.StartWatching();

        _accentColorWatcher.RegistryChanged += UpdateAccentColor;
        _accentColorWatcher.StartWatching();

        StartListenLockState();
    }

    public override void Disable()
    {
        _nightLightStateWatcher.StopWatching();
        _nightLightStateWatcher.RegistryChanged -= UpdateNightLight;

        _accentColorWatcher.StopWatching();
        _accentColorWatcher.RegistryChanged -= UpdateAccentColor;
        
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

    private void UpdateNightLight(object? sender, RegistryChangedEventArgs registryChangedEventArgs)
    {
        var data = registryChangedEventArgs.Data;
        if (data is null)
        {
            DataModel.NightLightsEnabled = false;
            return;
        }

        var byteData = (byte[])data;
        DataModel.NightLightsEnabled = byteData.Length > 24 && byteData[23] == 0x10 && byteData[24] == 0x00;
    }

    private void UpdateAccentColor(object? sender, RegistryChangedEventArgs registryChangedEventArgs)
    {
        var data = registryChangedEventArgs.Data;
        switch (data)
        {
            case null:
                DataModel.AccentColor = SKColor.Empty;
                return;
            case int accentColorDword:
                DataModel.AccentColor = ParseDWordColor(accentColorDword);
                break;
        }
    }

    private static SKColor ParseDWordColor(int color)
    {
        var a = (byte)((color >> 24) & 0xFF);
        var b = (byte)((color >> 16) & 0xFF);
        var g = (byte)((color >> 8) & 0xFF);
        var r = (byte)((color >> 0) & 0xFF);

        return new SKColor(r, g, b, a);
    }
}