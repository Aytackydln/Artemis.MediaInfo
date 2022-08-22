using Artemis.Core;
using Artemis.Core.Modules;

namespace Artemis.MediaInfo.DataModels
{
    public class MediaInfoDataModel : DataModel
    {
        [DataModelProperty(Name = "A media is being reported")]
        public bool HasMedia { get; set; }
        
        [DataModelProperty(Name = "A media is playing")]
        public bool MediaPlaying { get; set; }
        
        [DataModelProperty(Name = "Next media can be played")]
        public bool HasNextMedia { get; set; }
        
        [DataModelProperty(Name = "Previous media can be played")]
        public bool HasPreviousMedia { get; set; }
    }
}
