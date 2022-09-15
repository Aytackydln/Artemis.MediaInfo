using Artemis.Core;
using Artemis.Core.Modules;
using System.Collections.Generic;
using System.Linq;
using Artemis.MediaInfo.DataModels;
using WindowsMediaController;
using System;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage.Streams;
using SkiaSharp;
using Artemis.Core.Services;
using Windows.Media.Control;
using static WindowsMediaController.MediaManager;

namespace Artemis.MediaInfo
{
    [PluginFeature(Name = "MediaInfo")]
    public class MediaInfoModule : Module<MediaInfoDataModel>
    {
        private readonly IColorQuantizerService _colorQuantizer;
        private MediaManager _mediaManager;

        public override List<IModuleActivationRequirement> ActivationRequirements { get; } = new();

        private readonly HashSet<MediaSession> _mediaSessions = new(new MediaSessionComparer());
        private readonly HashSet<MediaSession> _albumArtSessions = new(new MediaSessionComparer());

        public MediaInfoModule(IColorQuantizerService colorQuantizerService)
        {
            _colorQuantizer = colorQuantizerService;
        }

        public override void Enable()
        {
            _mediaManager = new MediaManager();
            _mediaManager.OnAnySessionOpened += MediaManager_OnSessionOpened;
            _mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            _mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;

            _mediaManager.Start();

            DataModel.HasMedia = _mediaManager.CurrentMediaSessions.Count > 0;
            if (!DataModel.HasMedia) return;

            _mediaSessions.Clear();
            _albumArtSessions.Clear();
            foreach (var (_, mediaSession) in _mediaManager.CurrentMediaSessions)
            {
                _mediaSessions.Add(mediaSession);
                mediaSession.ControlSession.TryGetMediaPropertiesAsync().AsTask().ContinueWith(task =>
                {
                    if (task.Result.Thumbnail != null)
                    {
                        _albumArtSessions.Add(mediaSession);
                    }
                });
            }

            DataModel.MediaSessions = _mediaSessions;   //debug
            
            UpdateButtons();
        }

        public override void Disable()
        {
            foreach (var (_, mediaSession) in _mediaManager.CurrentMediaSessions)
            {
                _mediaSessions.Remove(mediaSession);
            }
            
            _mediaManager.OnAnySessionOpened -= MediaManager_OnSessionOpened;
            _mediaManager.OnAnyPlaybackStateChanged -= MediaManager_OnAnyPlaybackStateChanged;
            _mediaManager.OnAnyMediaPropertyChanged -= MediaManager_OnAnyMediaPropertyChanged;
            _mediaManager.OnAnySessionClosed -= MediaManager_OnAnySessionClosed;
            _mediaManager.Dispose();
            _mediaManager = null;
        }

        public override void Update(double deltaTime) {}
        public override void ModuleActivated(bool isOverride) {}
        public override void ModuleDeactivated(bool isOverride) {}

        private void MediaManager_OnSessionOpened(MediaSession mediaSession)
        {
            DataModel.HasMedia = true;
            _mediaSessions.Add(mediaSession);

            UpdateButtons();
        }

        private void MediaManager_OnAnySessionClosed(MediaSession mediaSession)
        {
            _mediaSessions.Remove(mediaSession);
            _albumArtSessions.Remove(mediaSession);

            UpdateButtons();
        }

        private async void MediaManager_OnAnyMediaPropertyChanged(MediaSession mediaSession,
            GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            DataModel.MediaChanged.Trigger(new MediaChangedEventArgs
            {
                SessionId = mediaSession.Id,
                Title = mediaProperties.Title,
                Artist = mediaProperties.Artist,
                MediaType = mediaProperties.PlaybackType ?? MediaPlaybackType.Unknown,
                HasArt = mediaProperties.Thumbnail is not null
            });
            
            try
            {
                if (mediaProperties.Thumbnail is null)
                {
                    _albumArtSessions.Remove(mediaSession);
                    DataModel.HasArt = _albumArtSessions.Count > 0;
                    return;
                }

                DataModel.ArtColors = await ReadMediaColors(mediaProperties);
                DataModel.HasArt = true;
                _albumArtSessions.Add(mediaSession);
            }
            catch
            {
                DataModel.HasArt = false;
                _albumArtSessions.Remove(mediaSession);
                DataModel.HasArt = _albumArtSessions.Count > 0;
            }
        }

        private async Task<ColorSwatch> ReadMediaColors(GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            var imageStream = await mediaProperties.Thumbnail.OpenReadAsync();
            var fileBytes = new byte[imageStream.Size];

            using (var reader = new DataReader(imageStream))
            {
                await reader.LoadAsync((uint)imageStream.Size);
                reader.ReadBytes(fileBytes);
            }

            using SKBitmap bitmap = SKBitmap.Decode(fileBytes);
            SKColor[] skClrs = _colorQuantizer.Quantize(bitmap.Pixels, 256);
            return _colorQuantizer.FindAllColorVariations(skClrs, true);;
        }

        private void MediaManager_OnAnyPlaybackStateChanged(MediaSession mediaSession,
            GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo)
        {
            UpdateButtons();
            DataModel.MediaState = playbackInfo.PlaybackStatus;
        }

        private void UpdateButtons()
        {
            DataModel.HasMedia = _mediaSessions.Count > 0;
            DataModel.HasNextMedia = _mediaSessions.Any(
                value => value.ControlSession.GetPlaybackInfo().Controls.IsNextEnabled);
            DataModel.HasPreviousMedia = _mediaSessions.Any(value =>
                value.ControlSession.GetPlaybackInfo().Controls.IsPreviousEnabled);
            DataModel.MediaPlaying = _mediaSessions.Any(value =>
                value.ControlSession.GetPlaybackInfo().PlaybackStatus ==
                GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);
            DataModel.HasArt = _albumArtSessions.Count > 0;
        }

        private class MediaSessionComparer : IEqualityComparer<MediaSession>
        {
            public bool Equals(MediaSession x, MediaSession y)
            {
                return x?.Id == y?.Id;
            }

            public int GetHashCode(MediaSession obj)
            {
                return obj.Id.GetHashCode();
            }
        }
    }
}