using Artemis.Core.ColorScience;
using Artemis.Core.Modules;
using SkiaSharp;

namespace Artemis.MediaInfo.DataModels;

public class WindowsInfoDataModel : DataModel
{
    [DataModelProperty(Name = "Session Is Locked")]
    public bool SessionLocked { get; set; }
        
    [DataModelProperty(Name = "Night Lights Enabled")]
    public bool NightLightsEnabled { get; set; }
    
    [DataModelProperty(Name = "Desktop Accent Color")]
    public SKColor AccentColor { get; set; } = SKColor.Empty;
        
    [DataModelProperty(Name = "Monitors Off")]
    public bool MonitorsOff { get; set; }
}