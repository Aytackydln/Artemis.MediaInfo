using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;
using WindowsMediaController;
using static WindowsMediaController.MediaManager;

namespace Artemis.MediaInfo.MediaWatch;

/// <summary>
/// Wrapping class that it's only dependency is Dubya.WindowsMediaController
/// </summary>
public class MediaWatcher
{
    private readonly MediaManager _mediaManager = new();

    public ISet<MediaSession> MediaSessions { get; } = new HashSet<MediaSession>(new MediaSessionComparer());
    public ISet<MediaSession> AlbumArtSessions { get; } = new HashSet<MediaSession>(new MediaSessionComparer());

    private MediaSession? _currentSession;

    public event EventHandler<FocusedMediaChangedEventArgs>? FocusedMediaChanged;
    public event EventHandler<ArtStateChangedEventArgs>? ArtStateChanged;

    public async Task StartListening()
    {
        _mediaManager.OnAnySessionOpened += MediaManager_OnSessionOpened;
        _mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
        _mediaManager.OnFocusedSessionChanged += MediaManagerOnOnFocusedSessionChanged;
        _mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;  //listen for media arts

        await _mediaManager.StartAsync();

        foreach (var (_, mediaSession) in _mediaManager.CurrentMediaSessions)
        {
            MediaSessions.Add(mediaSession);
        }
    }

    public void StopListening()
    {
        MediaSessions.Clear();
        AlbumArtSessions.Clear();

        if (_currentSession != null)
        {
            _currentSession.OnPlaybackStateChanged -= MediaSession_OnPlaybackStateChanged;
        }
        _currentSession = null;

        _mediaManager.OnAnyMediaPropertyChanged -= MediaManager_OnAnyMediaPropertyChanged;
        _mediaManager.OnFocusedSessionChanged -= MediaManagerOnOnFocusedSessionChanged;
        _mediaManager.OnAnySessionClosed -= MediaManager_OnAnySessionClosed;
        _mediaManager.OnAnySessionOpened -= MediaManager_OnSessionOpened;
        _mediaManager.Dispose();
    }

    private void MediaManagerOnOnFocusedSessionChanged(MediaSession? mediaSession)
    {
        if (_currentSession != null)
        {
            _currentSession.OnPlaybackStateChanged -= MediaSession_OnPlaybackStateChanged;
        }

        if (mediaSession != null)
        {
            _currentSession = mediaSession;
            _currentSession.OnPlaybackStateChanged += MediaSession_OnPlaybackStateChanged;
        }
        FocusedMediaChanged?.Invoke(this, new FocusedMediaChangedEventArgs(mediaSession, mediaSession?.ControlSession.GetPlaybackInfo()));
    }

    private void MediaManager_OnSessionOpened(MediaSession mediaSession)
    {
        MediaSessions.Add(mediaSession);
        _mediaManager.ForceUpdate();
    }

    private void MediaManager_OnAnySessionClosed(MediaSession mediaSession)
    {
        var artSessionRemoved = AlbumArtSessions.Remove(mediaSession);
        if (artSessionRemoved)
        {
            NotifyNextMediaSession();
        }
        MediaSessions.Remove(mediaSession);
        _mediaManager.ForceUpdate();
    }

    private void MediaManager_OnAnyMediaPropertyChanged(MediaSession mediaSession,
        GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
    {
        if (mediaSession.ControlSession == null)
        {
            MediaManager_OnAnySessionClosed(mediaSession);
            return;
        }
        try
        {
            if (mediaProperties.Thumbnail is null)
            {
                AlbumArtSessions.Remove(mediaSession);
                NotifyNextMediaSession();
                return;
            }

            AlbumArtSessions.Add(mediaSession);
            ArtStateChanged?.Invoke(this, new ArtStateChangedEventArgs(mediaProperties.Thumbnail));
        }
        catch
        {
            AlbumArtSessions.Remove(mediaSession);
            NotifyNextMediaSession();
        }
    }

    private void NotifyNextMediaSession()
    {
        var nextArtSession = AlbumArtSessions.LastOrDefault();
        if (nextArtSession != null)
        {
            var mediaProperties = nextArtSession.ControlSession.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
            ArtStateChanged?.Invoke(this, new ArtStateChangedEventArgs(mediaProperties.Thumbnail));
        }
        else
        {
            ArtStateChanged?.Invoke(this, new ArtStateChangedEventArgs(null));
        }
    }

    private void MediaSession_OnPlaybackStateChanged(MediaSession mediaSession,
        GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
    {
        if (playbackInfo == null || mediaSession.ControlSession == null ||
            playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed ||
            playbackInfo.Controls == null)
        {
            MediaManager_OnAnySessionClosed(mediaSession);
            return;
        }
        
        // Some app like chrome use same id for different tabs.
        // So, closing the tab may cause all chroma sessions to be removed. This re-adds them
        MediaSessions.Add(mediaSession);

        FocusedMediaChanged?.Invoke(this, new FocusedMediaChangedEventArgs(mediaSession, playbackInfo));
    }

    private sealed class MediaSessionComparer : IEqualityComparer<MediaSession>
    {
        public bool Equals(MediaSession? x, MediaSession? y)
        {
            return x?.Id == y?.Id;
        }

        public int GetHashCode(MediaSession obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}