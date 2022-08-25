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
        private readonly MediaManager _mediaManager = new();

        public override List<IModuleActivationRequirement> ActivationRequirements { get; } = new();

        private readonly List<string> _mediaSessions = new();

        public override void Enable()
        {
            _mediaManager.OnAnySessionOpened += MediaManager_OnSessionOpened;
            _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            _mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;

            _mediaManager.Start();
            while(!_mediaManager.IsStarted){}
            
            DataModel.HasMedia = _mediaManager.CurrentMediaSessions.Count > 0;

            if (_mediaManager.CurrentMediaSessions.Count == 0) return;
            
            
            DataModel.HasNextMedia = _mediaManager.CurrentMediaSessions.Any(pair => pair.Value.ControlSession.GetPlaybackInfo().Controls.IsNextEnabled);
            DataModel.HasPreviousMedia = _mediaManager.CurrentMediaSessions.Any(pair => pair.Value.ControlSession.GetPlaybackInfo().Controls.IsPreviousEnabled);
            DataModel.MediaPlaying = _mediaManager.CurrentMediaSessions.Any(pair => pair.Value.ControlSession.GetPlaybackInfo().PlaybackStatus ==
                                                                         GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
        }

        public override void Disable()
        {
            _mediaManager.OnAnySessionOpened -= MediaManager_OnSessionOpened;
            _mediaManager.OnAnySessionClosed -= MediaManager_OnAnySessionClosed;
            _mediaManager.OnAnyPlaybackStateChanged -= MediaManager_OnAnyPlaybackStateChanged;
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

        private void MediaManager_OnSessionOpened(MediaManager.MediaSession mediasession)
        {
            DataModel.HasMedia = true;
            
            _mediaSessions.Add(mediasession.Id);

            mediasession.OnSessionClosed += MediaManager_OnAnySessionClosed;
        }

        private void MediaManager_OnAnySessionClosed(MediaManager.MediaSession mediasession)
        {
            mediasession.OnSessionClosed -= MediaManager_OnAnySessionClosed;
            _mediaSessions.Remove(mediasession.Id);

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

        private void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession mediasession, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackinfo)
        {
            UpdateButtons();
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