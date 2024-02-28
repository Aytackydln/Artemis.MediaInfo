using System;
using System.Collections.Generic;
using Artemis.Core.Modules;
using Artemis.MediaInfo.DataModels;
using Artemis.MediaInfo.Utils;
using Microsoft.Win32;
using SkiaSharp;

namespace Artemis.MediaInfo;

public class WindowsInfoModule : Module<WindowsInfoDataModel>
{
    public override List<IModuleActivationRequirement> ActivationRequirements { get; } = [];

    private readonly RegistryWatcher _nightLightStateWatcher = new(WatchedRegistry.CurrentUser,
        @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CloudStore\\Store\\DefaultAccount\\Current\\default$windows.data.bluelightreduction.bluelightreductionstate\\windows.data.bluelightreduction.bluelightreductionstate",
        "Data");
    private readonly RegistryWatcher _nightLightSettingsWatcher = new(WatchedRegistry.CurrentUser,
        @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CloudStore\\Store\\DefaultAccount\\Current\\default$windows.data.bluelightreduction.settings\\windows.data.bluelightreduction.settings",
        "Data");
    private readonly RegistryWatcher _accentColorWatcher = new(WatchedRegistry.CurrentUser,
        @"SOFTWARE\\Microsoft\\Windows\\DWM",
        "AccentColor");

    public override void Enable()
    {
        _nightLightStateWatcher.RegistryChanged += UpdateNightLight;
        _nightLightStateWatcher.StartWatching();
        
        _nightLightSettingsWatcher.RegistryChanged += NightLightSettingsWatcherOnRegistryChanged;
        _nightLightSettingsWatcher.StartWatching();

        _accentColorWatcher.RegistryChanged += UpdateAccentColor;
        _accentColorWatcher.StartWatching();

        SystemEvents.SessionSwitch += OnSessionSwitch;
        DataModel.Username = GetUsername();
    }

    public override void Disable()
    {
        _nightLightStateWatcher.StopWatching();
        _nightLightStateWatcher.RegistryChanged -= UpdateNightLight;

        _nightLightSettingsWatcher.RegistryChanged -= NightLightSettingsWatcherOnRegistryChanged;
        _nightLightSettingsWatcher.StopWatching();

        _accentColorWatcher.StopWatching();
        _accentColorWatcher.RegistryChanged -= UpdateAccentColor;
        
        SystemEvents.SessionSwitch -= OnSessionSwitch;
    }

    public override void Update(double deltaTime)
    {
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        DataModel.SessionLocked = e.Reason switch
        {
            SessionSwitchReason.SessionLock => true,
            SessionSwitchReason.SessionUnlock => false,
            _ => DataModel.SessionLocked
        };
        DataModel.Username = e.Reason switch
        {
            SessionSwitchReason.SessionUnlock => GetUsername(),
            SessionSwitchReason.SessionLogon => GetUsername(),
            SessionSwitchReason.SessionLock => string.Empty,
            SessionSwitchReason.SessionLogoff => string.Empty,
            _ => DataModel.Username
        };
    }

    private static string GetUsername()
    {
        return System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split("\\")[1];
    }

    private void UpdateNightLight(object? sender, RegistryChangedEventArgs registryChangedEventArgs)
    {
        var data = registryChangedEventArgs.Data;
        if (data is not byte[] byteData)
        {
            DataModel.NightLightsEnabled = false;
            return;
        }

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

    private void NightLightSettingsWatcherOnRegistryChanged(object? sender, RegistryChangedEventArgs e)
    {
        var data = e.Data;
        if (data is null)
        {
            DataModel.NightLightsStrength = 0;
            return;
        }

        var byteData = (byte[])data;
        DataModel.NightLightsStrength = ParseNightLightStrength(byteData);
    }

    private static SKColor ParseDWordColor(int color)
    {
        var a = (byte)((color >> 24) & 0xFF);
        var b = (byte)((color >> 16) & 0xFF);
        var g = (byte)((color >> 8) & 0xFF);
        var r = (byte)((color >> 0) & 0xFF);

        return new SKColor(r, g, b, a);
    }

    private static double ParseNightLightStrength(byte[] data)
    {
        const int min = 4832;
        const int max = 26056;
        if (data.Length < 37)
        {
            return 0;
        }
        var value = BitConverter.ToInt16(data, 35);
        return 1 - (double)(value - min) / (max - min);
    }
}