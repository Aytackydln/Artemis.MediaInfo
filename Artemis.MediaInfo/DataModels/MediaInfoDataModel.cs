using Windows.Media.Control;
using Artemis.Core;
using Artemis.Core.Modules;
using Artemis.Core.Services;

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
        
        [DataModelProperty(Name = "Latest updated media state",
            Description = " Note that there may be other media sessions. " +
                          "Other values become true when any of the sessions meet the conditions.")]
        public GlobalSystemMediaTransportControlsSessionPlaybackStatus MediaState { get; set; }
        
        
        [DataModelProperty(Name = "Has Art",
            Description = "If the media app provided an art.")]
        public bool HasArt { get; set; }

        public ColorSwatch ArtColors { get; set; }
    }
}
