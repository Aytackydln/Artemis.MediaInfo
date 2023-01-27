using Artemis.Core;
using Artemis.Core.Modules;
using System.Collections.Generic;
using System.Linq;
using Artemis.MediaInfo.DataModels;
using WindowsMediaController;
using Windows.Media;
using Windows.Media.Control;
using Artemis.Core.Properties;
using static WindowsMediaController.MediaManager;
using static Artemis.MediaInfo.MediaInfoHelper;

namespace Artemis.MediaInfo;

[PluginFeature(Name = "MediaInfo")]
public class MediaInfoModule : Module<MediaInfoDataModel>
{
    private MediaManager _mediaManager;

    public override List<IModuleActivationRequirement> ActivationRequirements { get; } = new();

    private readonly HashSet<MediaSession> _mediaSessions = new(new MediaSessionComparer());
    private readonly HashSet<MediaSession> _albumArtSessions = new(new MediaSessionComparer());

    [CanBeNull]
    private MediaSession _currentSession;

    public override void Enable()
    {
        _mediaSessions.Clear();
        _albumArtSessions.Clear();

        _mediaManager = new MediaManager();
        _mediaManager.OnAnySessionOpened += MediaManager_OnSessionOpened;
        _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
        _mediaManager.OnFocusedSessionChanged += MediaManagerOnOnFocusedSessionChanged;
        _mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;  //listen for media arts

        _mediaManager.Start();

        foreach (var (_, mediaSession) in _mediaManager.CurrentMediaSessions)
        {
            _mediaSessions.Add(mediaSession);
        }
    }

    private void MediaManagerOnOnFocusedSessionChanged(MediaSession mediaSession)
    {
        if (_currentSession != null)
        {
            _currentSession.OnPlaybackStateChanged -= MediaSession_OnPlaybackStateChanged;
        }
        _mediaSessions.Add(mediaSession);
        _currentSession = mediaSession;
        _mediaManager.OnAnyPlaybackStateChanged += MediaSession_OnPlaybackStateChanged;
        MediaSession_OnPlaybackStateChanged(mediaSession, mediaSession.ControlSession.GetPlaybackInfo());
    }

    public override void Disable()
    {
        _albumArtSessions.Clear();
        _mediaSessions.Clear();

        if (_currentSession != null)
        {
            _currentSession.OnPlaybackStateChanged -= MediaSession_OnPlaybackStateChanged;
        }

        _mediaManager.OnAnyMediaPropertyChanged -= MediaManager_OnAnyMediaPropertyChanged;
        _mediaManager.OnFocusedSessionChanged -= MediaManagerOnOnFocusedSessionChanged;
        _mediaManager.OnAnySessionClosed -= MediaManager_OnAnySessionClosed;
        _mediaManager.OnAnySessionOpened -= MediaManager_OnSessionOpened;
        _mediaManager.Dispose();
        _mediaManager = null;
    }

    public override void Update(double deltaTime) {}

    public override void ModuleActivated(bool isOverride)
    {
        DataModel.MediaSessions = _mediaSessions;
        DataModel.ArtMediaSessions = _albumArtSessions;
    }
    public override void ModuleDeactivated(bool isOverride) {
        //unused
    }

    private void MediaManager_OnSessionOpened(MediaSession mediaSession)
    {
        DataModel.HasMedia = true;
        _mediaSessions.Add(mediaSession);
    }

    private void MediaManager_OnAnySessionClosed(MediaSession mediaSession)
    {
        _currentSession.OnPlaybackStateChanged -= MediaSession_OnPlaybackStateChanged;
        _mediaSessions.Remove(mediaSession);
        _albumArtSessions.Remove(mediaSession);
        if (_currentSession.Id == mediaSession.Id)
        {
            _currentSession = _mediaSessions.First();
        }
        UpdateButtons(_currentSession);
        UpdateArtState();
    }

    private async void MediaManager_OnAnyMediaPropertyChanged(MediaSession mediaSession,
        GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
    {
        try
        {
            if (mediaProperties.Thumbnail is null)
            {
                _albumArtSessions.Remove(mediaSession);
                return;
            }

            DataModel.ArtColors = await ReadMediaColors(mediaProperties);
            _albumArtSessions.Add(mediaSession);
        }
        catch
        {
            _albumArtSessions.Remove(mediaSession);
        }
        finally
        {
            UpdateArtState();
            DataModel.MediaChanged.Trigger(new MediaChangedEventArgs
            {
                SessionId = mediaSession.Id,
                Title = mediaProperties.Title,
                Artist = mediaProperties.Artist,
                MediaType = mediaProperties.PlaybackType ?? MediaPlaybackType.Unknown,
                HasArt = mediaProperties.Thumbnail is not null
            });
        }
    }

    private void MediaSession_OnPlaybackStateChanged(MediaSession mediaSession,
        GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo)
    {
        if (playbackInfo == null || playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed)
        {
            return;
        }

        _mediaSessions.Add(mediaSession);   // Some app like chrome use same id for different tabs.
                                            // So, closing the tab may cause all chroma sessions to be removed. This re-adds them

        UpdateButtons(mediaSession);
        UpdateArtState();
    }

    private void UpdateButtons(MediaSession mediaSession)
    {
        if (mediaSession == null)
        {
            DataModel.HasMedia = false;
            DataModel.HasNextMedia = false;
            DataModel.HasPreviousMedia = false;
            DataModel.MediaPlaying = false;
            DataModel.MediaState = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed;
            DataModel.SessionName = "";
            return;
        }
        DataModel.HasMedia = true;

        var playbackInfo = mediaSession.ControlSession.GetPlaybackInfo();
        var playbackControls = playbackInfo.Controls;
        DataModel.HasNextMedia = playbackControls.IsNextEnabled;
        DataModel.HasPreviousMedia = playbackControls.IsPreviousEnabled;
        DataModel.MediaPlaying = playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        DataModel.MediaState = playbackInfo.PlaybackStatus;
        DataModel.SessionName = mediaSession.Id;
    }

    private void UpdateArtState()
    {
        _albumArtSessions.TrimExcess();
        DataModel.HasArt = _albumArtSessions.Any();
    }

    private sealed class MediaSessionComparer : IEqualityComparer<MediaSession>
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