using System;
using System.Management;
using System.Security.Principal;
using Microsoft.Win32;

namespace Artemis.MediaInfo.Utils;

public class RegistryChangedEventArgs : EventArgs
{
    public readonly object Data;

    public RegistryChangedEventArgs(object data)
    {
        Data = data;
    }
}

public sealed class RegistryWatcher : IDisposable
{
    public event EventHandler<RegistryChangedEventArgs> RegistryChanged;
    
    private readonly string _key;
    private readonly string _value;
    private ManagementEventWatcher _eventWatcher;

    public RegistryWatcher(string key, string value)
    {
        _key = key;
        _value = value;
    }

    public void StartWatching()
    {
        var currentUser = WindowsIdentity.GetCurrent();
        ManagementScope scope = new ManagementScope("\\\\.\\root\\default");
        var queryString = string.Format(
            "SELECT * FROM RegistryValueChangeEvent WHERE Hive='HKEY_USERS' AND KeyPath='{0}\\\\{1}' AND ValueName='{2}'",
            currentUser.User.Value, _key.Replace("\\", "\\\\"), _value);
        var query = new WqlEventQuery(queryString);
        _eventWatcher = new ManagementEventWatcher(scope, query);
        _eventWatcher.EventArrived += KeyWatcherOnEventArrived;
        _eventWatcher.Start();
        
        SendData();
    }

    public void StopWatching()
    {
        _eventWatcher.EventArrived -= KeyWatcherOnEventArrived;
        _eventWatcher.Stop();
        _eventWatcher.Dispose();
        _eventWatcher = null;
    }

    private void KeyWatcherOnEventArrived(object sender, EventArrivedEventArgs e)
    {
        SendData();
    }

    private void SendData()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_key);
        var data = key?.GetValue(_value);
        RegistryChanged?.Invoke(this, new RegistryChangedEventArgs(data));
    }

    public void Dispose()
    {
        StopWatching();
    }
}