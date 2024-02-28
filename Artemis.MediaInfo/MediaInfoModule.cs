using Artemis.Core;
using Artemis.Core.Modules;
using System.Collections.Generic;
using Windows.Media;
using Artemis.MediaInfo.DataModels;
using Windows.Media.Control;
using Artemis.MediaInfo.MediaWatch;
using static WindowsMediaController.MediaManager;
using static Artemis.MediaInfo.Utils.MediaInfoHelper;

namespace Artemis.MediaInfo;

[PluginFeature(Name = "MediaInfo")]
public class MediaInfoModule : Module<MediaInfoDataModel>
{
    private static readonly bool FocusedUpdate = false;   //because window's focus events don't work well for everyone :/

    public override List<IModuleActivationRequirement> ActivationRequirements { get; } = [];

    private readonly MediaWatcher _mediaWatcher = new();

    public override void Enable()
    {
        DataModel.MediaSessions = _mediaWatcher.MediaSessions;
        DataModel.ArtMediaSessions = _mediaWatcher.AlbumArtSessions;
    }

    public override void Disable()
    {
        //unused
    }

    public override void Update(double deltaTime) {}

    public override void ModuleActivated(bool isOverride)
    {
        _mediaWatcher.FocusedMediaChanged += MediaWatcherOnFocusedMediaChanged;
        _mediaWatcher.ArtStateChanged += MediaWatcherOnArtStateChanged;
        _mediaWatcher.StartListening().Wait();
    }
    public override void ModuleDeactivated(bool isOverride) {
        _mediaWatcher.StopListening();
        _mediaWatcher.FocusedMediaChanged -= MediaWatcherOnFocusedMediaChanged;
        _mediaWatcher.ArtStateChanged -= MediaWatcherOnArtStateChanged;
    }

    private void MediaWatcherOnFocusedMediaChanged(object? sender, FocusedMediaChangedEventArgs e)
    {
        var mediaSession = e.MediaSession;
        UpdateButtons(mediaSession, e.PlaybackInfo);
    }

    private void UpdateButtons(MediaSession? mediaSession,
        GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
    {
        if (mediaSession == null)
        {
            DataModel.HasMedia = false;
            DataModel.HasNextMedia = false;
            DataModel.HasPreviousMedia = false;
            DataModel.MediaPlaying = false;
            DataModel.MediaState = GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed;
            DataModel.MediaType = MediaPlaybackType.Unknown;
            DataModel.SessionName = "";
            return;
        }
        DataModel.HasMedia = true;

        if (FocusedUpdate)
        {
            FocusedModelUpdate(mediaSession, playbackInfo);
        }
        else
        {
            AnySessionModelUpdate(mediaSession, playbackInfo);
        }
    }

    private void FocusedModelUpdate(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
    {
        if (playbackInfo == null)
        {
            playbackInfo = mediaSession.ControlSession.GetPlaybackInfo();
        }

        var playbackControls = playbackInfo.Controls;

        DataModel.HasNextMedia = playbackControls.IsNextEnabled;
        DataModel.HasPreviousMedia = playbackControls.IsPreviousEnabled;
        DataModel.MediaPlaying =
            playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
        DataModel.MediaState = playbackInfo.PlaybackStatus;
        DataModel.MediaType = playbackInfo.PlaybackType ?? MediaPlaybackType.Unknown;
        DataModel.SessionName = mediaSession.Id;
    }

    private void AnySessionModelUpdate(MediaSession focusedMediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo)
    {
        DataModel.HasPreviousMedia = false;
        DataModel.MediaPlaying = false;
        DataModel.HasNextMedia = false;
        DataModel.MediaState = playbackInfo?.PlaybackStatus ?? GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed;
        DataModel.MediaType = playbackInfo?.PlaybackType ?? MediaPlaybackType.Unknown;
        DataModel.SessionName = focusedMediaSession.Id;
        foreach (var mediaSession in _mediaWatcher.MediaSessions)
        {
            var controls = mediaSession.ControlSession.GetPlaybackInfo().Controls;
            DataModel.HasPreviousMedia |= controls.IsPreviousEnabled;
            DataModel.MediaPlaying |= !controls.IsPlayEnabled;
            DataModel.HasNextMedia |= controls.IsNextEnabled;
        }
    }

    private async void MediaWatcherOnArtStateChanged(object? sender, ArtStateChangedEventArgs e)
    {
        if (e.Thumbnail != null)
        {
            DataModel.HasArt = true;
            DataModel.ArtColors = await ReadMediaColors(e.Thumbnail);
        }
        else
        {
            DataModel.HasArt = false;
        }
    }
}