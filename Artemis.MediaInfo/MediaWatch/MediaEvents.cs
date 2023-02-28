using System;
using Windows.Media.Control;
using Windows.Storage.Streams;
using WindowsMediaController;

namespace Artemis.MediaInfo.MediaWatch;

public class MediaAddedEventArgs : EventArgs
{
    public MediaManager.MediaSession MediaSession { get; }

    public MediaAddedEventArgs(MediaManager.MediaSession mediaSession)
    {
        MediaSession = mediaSession;
    }
}

public class ArtStateChangedEventArgs : EventArgs
{
    public IRandomAccessStreamReference? Thumbnail { get; }

    public ArtStateChangedEventArgs(IRandomAccessStreamReference? thumbnail)
    {
        Thumbnail = thumbnail;
    }
}

public class FocusedMediaChangedEventArgs : EventArgs
{
    public MediaManager.MediaSession? MediaSession { get; }
    public GlobalSystemMediaTransportControlsSessionPlaybackInfo? PlaybackInfo { get; }

    public FocusedMediaChangedEventArgs(MediaManager.MediaSession? mediaSession,
        GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
    {
        MediaSession = mediaSession;
        PlaybackInfo = playbackInfo;
    }
}