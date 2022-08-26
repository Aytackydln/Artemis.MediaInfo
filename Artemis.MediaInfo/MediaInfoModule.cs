using Artemis.Core;
using Artemis.Core.Modules;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Control;
using Artemis.MediaInfo.DataModels;
using WindowsMediaController;

namespace Artemis.MediaInfo
{
    [PluginFeature(Name = "MediaInfo")]
    public class MediaInfoModule : Module<MediaInfoDataModel>
    {
        private MediaManager _mediaManager;

        public override List<IModuleActivationRequirement> ActivationRequirements { get; } = new();

        private List<string> _mediaSessions = new();

        public override void Enable()
        {
            _mediaManager = new MediaManager();
            _mediaManager.OnAnySessionOpened += MediaManager_OnSessionOpened;
            _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            _mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;

            _mediaManager.Start();

            DataModel.HasMedia = _mediaManager.CurrentMediaSessions.Count > 0;

            if (_mediaManager.CurrentMediaSessions.Count == 0) return;
            _mediaSessions = new List<string>(_mediaManager.CurrentMediaSessions.Select(pair => pair.Value.Id));
            
            DataModel.HasNextMedia = _mediaManager.CurrentMediaSessions.Any(pair => pair.Value.ControlSession.GetPlaybackInfo().Controls.IsNextEnabled);
            DataModel.HasPreviousMedia = _mediaManager.CurrentMediaSessions.Any(pair => pair.Value.ControlSession.GetPlaybackInfo().Controls.IsPreviousEnabled);
            DataModel.MediaPlaying = _mediaManager.CurrentMediaSessions.Any(pair => pair.Value.ControlSession.GetPlaybackInfo().PlaybackStatus ==
                                                                         GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
        }

        public override void Disable()
        {
            foreach (var (_, mediaSession) in _mediaManager.CurrentMediaSessions)
            {
                mediaSession.OnSessionClosed -= MediaManager_OnAnySessionClosed;
            }
            
            _mediaManager.OnAnySessionOpened -= MediaManager_OnSessionOpened;
            _mediaManager.OnAnySessionClosed -= MediaManager_OnAnySessionClosed;
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

        private void MediaManager_OnSessionOpened(MediaManager.MediaSession mediaSession)
        {
            DataModel.HasMedia = true;
            
            _mediaSessions.Add(mediaSession.Id);

            mediaSession.OnSessionClosed += MediaManager_OnAnySessionClosed;
        }

        private void MediaManager_OnAnySessionClosed(MediaManager.MediaSession mediaSession)
        {
            mediaSession.OnSessionClosed -= MediaManager_OnAnySessionClosed;
            _mediaSessions.Remove(mediaSession.Id);

            if (_mediaSessions.Count == 0)
            {
                DataModel.HasMedia = false;
                DataModel.HasNextMedia = false;
                DataModel.HasPreviousMedia = false;
            }
            else
            {
                UpdateButtons();
            }
        }

        private void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession mediaSession,
            GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo)
        {
            UpdateButtons();
            DataModel.MediaState = playbackInfo.PlaybackStatus;
        }

        private void UpdateButtons()
        {
            DataModel.HasNextMedia = _mediaManager.CurrentMediaSessions.Any(
                pair => pair.Value.ControlSession.GetPlaybackInfo().Controls.IsNextEnabled);
            DataModel.HasPreviousMedia = _mediaManager.CurrentMediaSessions.Any(pair =>
                pair.Value.ControlSession.GetPlaybackInfo().Controls.IsPreviousEnabled);
            DataModel.MediaPlaying = _mediaManager.CurrentMediaSessions.Any(pair =>
                pair.Value.ControlSession.GetPlaybackInfo().PlaybackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
        }
    }
}