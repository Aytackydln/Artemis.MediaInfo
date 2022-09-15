using Artemis.Core.Modules;

namespace Artemis.MediaInfo.DataModels;

public class WindowsInfoDataModel : DataModel
{
    [DataModelProperty(Name = "Session Is Locked")]
    public bool SessionLocked { get; set; }
        
    [DataModelProperty(Name = "Night Lights Enabled")]
    public bool NightLightsEnabled { get; set; }
}