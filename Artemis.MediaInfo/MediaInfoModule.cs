using Artemis.Core;
using Artemis.Core.Modules;
using System.Collections.Generic;
using System.Linq;
using Artemis.MediaInfo.DataModels;
using WindowsMediaController;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Storage.Streams;
using SkiaSharp;
using Artemis.Core.Services;
using Windows.Media.Control;

namespace Artemis.MediaInfo
{
    [PluginFeature(Name = "MediaInfo")]
    public class MediaInfoModule : Module<MediaInfoDataModel>
    {
        private readonly IColorQuantizerService _colorQuantizer;
        private MediaManager _mediaManager;

        public override List<IModuleActivationRequirement> ActivationRequirements { get; } = new();

        private List<string> _mediaSessions = new();

        public MediaInfoModule(IColorQuantizerService colorQuantizerService)
        {
            _colorQuantizer = colorQuantizerService;
        }

        public override void Enable()
        {
            _mediaManager = new MediaManager();
            _mediaManager.OnAnySessionOpened += MediaManager_OnSessionOpened;

            _mediaManager.Start();

            var mediaExist = _mediaManager.CurrentMediaSessions.Count > 0;
            DataModel.HasMedia = mediaExist;

            _mediaSessions = new List<string>();
            if (!mediaExist) return;

            foreach (var (_, mediaSession) in _mediaManager.CurrentMediaSessions)
            {
                _mediaSessions.Add(mediaSession.Id);
                mediaSession.OnMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
                mediaSession.OnPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
                mediaSession.OnSessionClosed += MediaManager_OnAnySessionClosed;
            }
            
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

            mediaSession.OnMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            mediaSession.OnPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            mediaSession.OnSessionClosed += MediaManager_OnAnySessionClosed;
        }

        private void MediaManager_OnAnySessionClosed(MediaManager.MediaSession mediaSession)
        {
            mediaSession.OnMediaPropertyChanged -= MediaManager_OnAnyMediaPropertyChanged;
            mediaSession.OnPlaybackStateChanged -= MediaManager_OnAnyPlaybackStateChanged;
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

        private async void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            try
            {
                if (mediaProperties.Thumbnail is null)
                {
                    DataModel.HasArt = false;
                    return;
                }

                var imageStream = await mediaProperties.Thumbnail.OpenReadAsync();
                var fileBytes = new byte[imageStream.Size];

                using DataReader reader = new DataReader(imageStream);
                await reader.LoadAsync((uint)imageStream.Size);
                reader.ReadBytes(fileBytes);

                using SKBitmap skbm = SKBitmap.Decode(fileBytes);
                SKColor[] skClrs = _colorQuantizer.Quantize(skbm.Pixels, 256);
                DataModel.ArtColors =  _colorQuantizer.FindAllColorVariations(skClrs, true);
                DataModel.HasArt = true;
            }
            catch
            {
                DataModel.HasArt = false;
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