using Artemis.Core;
using Artemis.Core.Modules;
using System.Collections.Generic;
using Windows.Media.Control;
using Artemis.MediaInfo.DataModels;
using WindowsMediaController;

namespace Artemis.MediaInfo
{
    [PluginFeature(Name = "MediaInfo")]
    public class MediaInfoModule : Module<MediaInfoDataModel>
    {
        private MediaManager _mediaManager = new();

        public override List<IModuleActivationRequirement> ActivationRequirements { get; } = new();

        public override void Enable()
        {
            _mediaManager = new MediaManager();
            _mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionChanged;
            _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionChanged;
            _mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;

            _mediaManager.Start();
            while(!_mediaManager.IsStarted){}
        }

        public override void Disable()
        {
            _mediaManager.OnAnySessionOpened -= MediaManager_OnAnySessionChanged;
            _mediaManager.OnAnySessionClosed -= MediaManager_OnAnySessionChanged;
            _mediaManager.OnAnyPlaybackStateChanged -= MediaManager_OnAnyPlaybackStateChanged;

            _mediaManager.Dispose();
            _mediaManager = null;
        }

        public override void Update(double deltaTime)
        {
            
        }

        public override void ModuleActivated(bool isOverride)
        {

        }

        public override void ModuleDeactivated(bool isOverride)
        {

        }

        private void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession mediasession, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackinfo)
        {
            var controls = playbackinfo.Controls;

            DataModel.MediaPlaying = playbackinfo.PlaybackStatus ==
                                     GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            DataModel.HasNextMedia = controls.IsNextEnabled;
            DataModel.HasPreviousMedia = controls.IsPreviousEnabled;
        }

        private void MediaManager_OnAnySessionChanged(MediaManager.MediaSession mediasession)
        {
            DataModel.HasMedia = _mediaManager.CurrentMediaSessions.Count > 0;
        }
    }
}