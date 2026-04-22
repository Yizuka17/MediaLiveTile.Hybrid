using MediaLiveTile.Hybrid.TrayHost.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Control;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal sealed class TrayMediaWatcherService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private bool _initialized;
        private bool _isRefreshing;
        private bool _refreshPending;

        private readonly List<GlobalSystemMediaTransportControlsSession> _subscribedSessions = new();
        private CancellationTokenSource? _debounceCts;

        public event EventHandler? StateChanged;

        public TrayMediaSelectionResult? LatestResult { get; private set; }

        public string StatusText { get; private set; } = "等待初始化";

        public DateTimeOffset? LastRefreshTime { get; private set; }

        public bool IsRefreshing => _isRefreshing;

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

            _manager.SessionsChanged += Manager_SessionsChanged;
            _manager.CurrentSessionChanged += Manager_CurrentSessionChanged;

            RebuildSessionSubscriptions();

            _initialized = true;

            await RefreshNowAsync();
        }

        public async Task RefreshNowAsync()
        {
            if (_isRefreshing)
            {
                _refreshPending = true;
                return;
            }

            _isRefreshing = true;
            StatusText = "正在读取系统媒体会话...";
            RaiseStateChanged();

            try
            {
                if (_manager == null)
                {
                    _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                    RebuildSessionSubscriptions();
                }

                var sessions = _manager.GetSessions();
                var currentSession = _manager.GetCurrentSession();

                string? currentAppId = null;
                string? currentTitle = null;
                string? currentArtist = null;

                if (currentSession != null)
                {
                    try
                    {
                        currentAppId = currentSession.SourceAppUserModelId;
                        var currentProps = await currentSession.TryGetMediaPropertiesAsync();
                        currentTitle = currentProps?.Title;
                        currentArtist = currentProps?.Artist;
                    }
                    catch
                    {
                    }
                }

                var rawList = new List<TrayMediaSessionInfo>();

                foreach (var session in sessions)
                {
                    string appId = session.SourceAppUserModelId ?? "Unknown";

                    try
                    {
                        var mediaProps = await session.TryGetMediaPropertiesAsync();
                        var playbackInfo = session.GetPlaybackInfo();
                        var playbackStatus = playbackInfo?.PlaybackStatus
                            ?? GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed;

                        string title = string.IsNullOrWhiteSpace(mediaProps?.Title) ? "无标题" : mediaProps.Title;
                        string artist = string.IsNullOrWhiteSpace(mediaProps?.Artist) ? "无艺人" : mediaProps.Artist;
                        string album = string.IsNullOrWhiteSpace(mediaProps?.AlbumTitle) ? "无专辑" : mediaProps.AlbumTitle;

                        var thumbnailUri = await LoadAndCacheThumbnailAsync(
                            mediaProps?.Thumbnail,
                            appId,
                            title,
                            artist,
                            album);

                        string? appIconUri = null;
                        if (string.IsNullOrWhiteSpace(thumbnailUri))
                        {
                            appIconUri = await LoadAndCacheAppIconAsync(appId);
                        }

                        var item = new TrayMediaSessionInfo
                        {
                            SourceAppUserModelId = appId,
                            SourceDisplayName = ResolveSourceDisplayName(appId),
                            Title = title,
                            Artist = artist,
                            AlbumTitle = album,
                            PlaybackStatus = playbackStatus.ToString(),
                            IsPlaying = playbackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                            ThumbnailLocalUri = thumbnailUri,
                            AppIconLocalUri = appIconUri
                        };

                        item.IsCurrentSession = IsCurrentSession(
                            item.SourceAppUserModelId,
                            item.Title,
                            item.Artist,
                            currentAppId,
                            currentTitle,
                            currentArtist);

                        item.IsMusicPreferred = DetectMusicPreferred(item);
                        item.InfoCompletenessScore = CalculateInfoCompletenessScore(item);

                        rawList.Add(item);
                    }
                    catch (Exception ex)
                    {
                        rawList.Add(new TrayMediaSessionInfo
                        {
                            SourceAppUserModelId = appId,
                            SourceDisplayName = ResolveSourceDisplayName(appId),
                            Title = "读取失败",
                            Artist = ex.Message,
                            AlbumTitle = "",
                            PlaybackStatus = "Error",
                            IsPlaying = false,
                            IsCurrentSession = false,
                            IsMusicPreferred = false,
                            InfoCompletenessScore = 0
                        });
                    }
                }

                var deduped = rawList
                    .GroupBy(x => $"{x.SourceAppUserModelId}|{x.Title}|{x.Artist}|{x.AlbumTitle}")
                    .Select(g => g
                        .OrderByDescending(x => x.IsPlaying)
                        .ThenByDescending(x => x.IsMusicPreferred)
                        .ThenByDescending(x => x.InfoCompletenessScore)
                        .ThenByDescending(x => x.IsCurrentSession)
                        .ThenByDescending(x => x.HasThumbnail)
                        .First())
                    .OrderByDescending(x => x.IsPlaying)
                    .ThenByDescending(x => x.IsMusicPreferred)
                    .ThenByDescending(x => x.InfoCompletenessScore)
                    .ThenByDescending(x => x.IsCurrentSession)
                    .ThenByDescending(x => x.HasThumbnail)
                    .ThenBy(x => x.SourceDisplayName)
                    .ToList();

                for (int i = 0; i < deduped.Count; i++)
                {
                    deduped[i].Order = i + 1;

                    if (i == 0)
                        deduped[i].Role = "主媒体";
                    else if (i == 1)
                        deduped[i].Role = "次媒体";
                    else
                        deduped[i].Role = "候选";
                }

                LatestResult = new TrayMediaSelectionResult
                {
                    AllSessions = deduped,
                    PrimaryMedia = deduped.Count > 0 ? deduped[0] : null,
                    SecondaryMedia = deduped.Count > 1 ? deduped[1] : null
                };

                StatusText = LatestResult.Count == 0
                    ? "未检测到媒体会话"
                    : $"已检测到 {LatestResult.Count} 个媒体会话";

                LastRefreshTime = DateTimeOffset.Now;
            }
            catch (Exception ex)
            {
                StatusText = $"刷新失败：{ex.Message}";
            }
            finally
            {
                _isRefreshing = false;
                RaiseStateChanged();

                if (_refreshPending)
                {
                    _refreshPending = false;
                    _ = RefreshNowAsync();
                }
            }
        }

        private void Manager_SessionsChanged(
            GlobalSystemMediaTransportControlsSessionManager sender,
            SessionsChangedEventArgs args)
        {
            RebuildSessionSubscriptions();
            QueueRefresh(250);
        }

        private void Manager_CurrentSessionChanged(
            GlobalSystemMediaTransportControlsSessionManager sender,
            CurrentSessionChangedEventArgs args)
        {
            RebuildSessionSubscriptions();
            QueueRefresh(250);
        }

        private void Session_MediaPropertiesChanged(
            GlobalSystemMediaTransportControlsSession sender,
            MediaPropertiesChangedEventArgs args)
        {
            QueueRefresh(400);
        }

        private void Session_PlaybackInfoChanged(
            GlobalSystemMediaTransportControlsSession sender,
            PlaybackInfoChangedEventArgs args)
        {
            QueueRefresh(300);
        }

        private void RebuildSessionSubscriptions()
        {
            UnsubscribeAllSessions();

            if (_manager == null)
                return;

            foreach (var session in _manager.GetSessions())
            {
                session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
                session.PlaybackInfoChanged += Session_PlaybackInfoChanged;

                _subscribedSessions.Add(session);
            }
        }

        private void UnsubscribeAllSessions()
        {
            foreach (var session in _subscribedSessions)
            {
                try
                {
                    session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                    session.PlaybackInfoChanged -= Session_PlaybackInfoChanged;
                }
                catch
                {
                }
            }

            _subscribedSessions.Clear();
        }

        private void QueueRefresh(int delayMilliseconds)
        {
            try
            {
                _debounceCts?.Cancel();
                _debounceCts?.Dispose();
            }
            catch
            {
            }

            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMilliseconds, token);

                    if (!token.IsCancellationRequested)
                    {
                        await RefreshNowAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch
                {
                }
            }, token);
        }

        private async Task<string?> LoadAndCacheThumbnailAsync(
            IRandomAccessStreamReference? thumbnail,
            string appId,
            string title,
            string artist,
            string album)
        {
            if (thumbnail == null)
                return null;

            try
            {
                string fileName = CreateStablePngFileName(
                    "thumb",
                    appId,
                    title,
                    artist,
                    album);

                return await CacheStreamReferenceAsPngAsync(thumbnail, "TrayThumbCache", fileName);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> LoadAndCacheAppIconAsync(string appUserModelId)
        {
            if (string.IsNullOrWhiteSpace(appUserModelId))
                return null;

            try
            {
                var appInfo = AppInfo.GetFromAppUserModelId(appUserModelId);
                if (appInfo != null)
                {
                    var logoRef = appInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(128, 128));
                    if (logoRef != null)
                    {
                        string fileName = CreateStablePngFileName("appicon", appUserModelId);
                        var localUri = await CacheStreamReferenceAsPngAsync(logoRef, "TrayAppIconCache", fileName);
                        if (!string.IsNullOrWhiteSpace(localUri))
                            return localUri;
                    }
                }
            }
            catch
            {
            }

            return GetKnownAppIconUri(appUserModelId);
        }

        private async Task<string?> CacheStreamReferenceAsPngAsync(
            IRandomAccessStreamReference streamReference,
            string folderName,
            string fileName)
        {
            using var stream = await streamReference.OpenReadAsync();

            var decoder = await BitmapDecoder.CreateAsync(stream);
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);

            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                folderName,
                CreationCollisionOption.OpenIfExists);

            var file = await folder.CreateFileAsync(
                fileName,
                CreationCollisionOption.ReplaceExisting);

            using (var output = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
                encoder.SetSoftwareBitmap(softwareBitmap);
                await encoder.FlushAsync();
            }

            softwareBitmap.Dispose();

            return $"ms-appdata:///local/{folderName}/{file.Name}";
        }

        private string CreateStablePngFileName(string prefix, params string[] parts)
        {
            string raw = string.Join("|", parts ?? Array.Empty<string>());
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(raw));
            return $"{prefix}_{Convert.ToHexString(hash)}.png";
        }

        private string? GetKnownAppIconUri(string appUserModelId)
        {
            if (string.IsNullOrWhiteSpace(appUserModelId))
                return null;

            var id = appUserModelId.ToLowerInvariant();

            if (id.StartsWith("1f8b0f94.122165ae053f")
                || id.Contains("netease")
                || id.Contains("cloudmusic"))
            {
                return "ms-appx:///Assets/AppIcons/netease.png";
            }

            return id switch
            {
                "msedge.exe" => "ms-appx:///Assets/AppIcons/msedge.png",
                "chrome.exe" => "ms-appx:///Assets/AppIcons/chrome.png",
                "firefox.exe" => "ms-appx:///Assets/AppIcons/firefox.png",
                _ => null
            };
        }

        private bool IsCurrentSession(
            string appId,
            string title,
            string artist,
            string? currentAppId,
            string? currentTitle,
            string? currentArtist)
        {
            if (string.IsNullOrWhiteSpace(currentAppId))
                return false;

            if (!string.Equals(appId, currentAppId, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(currentTitle))
                return true;

            bool sameTitle = string.Equals(Normalize(title), Normalize(currentTitle), StringComparison.OrdinalIgnoreCase);
            bool sameArtist = string.Equals(Normalize(artist), Normalize(currentArtist), StringComparison.OrdinalIgnoreCase);

            return sameTitle || sameArtist;
        }

        private bool DetectMusicPreferred(TrayMediaSessionInfo item)
        {
            var source = $"{item.SourceDisplayName}|{item.SourceAppUserModelId}".ToLowerInvariant();

            if (source.Contains("网易云")
                || source.Contains("netease")
                || source.Contains("cloudmusic")
                || source.Contains("spotify")
                || source.Contains("qqmusic")
                || source.Contains("groove")
                || source.Contains("foobar")
                || source.Contains("musicbee")
                || source.Contains("applemusic")
                || source.Contains("apple music"))
            {
                return true;
            }

            if (HasRealValue(item.Artist, "无艺人"))
                return true;

            if (HasRealValue(item.AlbumTitle, "无专辑"))
                return true;

            return false;
        }

        private int CalculateInfoCompletenessScore(TrayMediaSessionInfo item)
        {
            int score = 0;

            if (HasRealValue(item.Title, "无标题", "读取失败"))
                score += 2;

            if (HasRealValue(item.Artist, "无艺人"))
                score += 2;

            if (HasRealValue(item.AlbumTitle, "无专辑"))
                score += 1;

            if (!string.IsNullOrWhiteSpace(item.EffectiveImageUri))
                score += 2;

            return score;
        }

        private bool HasRealValue(string? value, params string[] placeholders)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            foreach (var p in placeholders)
            {
                if (string.Equals(value, p, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private string ResolveSourceDisplayName(string appUserModelId)
        {
            if (string.IsNullOrWhiteSpace(appUserModelId))
                return "未知来源";

            try
            {
                var appInfo = AppInfo.GetFromAppUserModelId(appUserModelId);
                var displayName = appInfo?.DisplayInfo?.DisplayName;

                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;
            }
            catch
            {
            }

            if (appUserModelId.StartsWith("1F8B0F94.122165AE053F", StringComparison.OrdinalIgnoreCase))
                return "网易云音乐";

            if (string.Equals(appUserModelId, "msedge.exe", StringComparison.OrdinalIgnoreCase))
                return "Microsoft Edge";

            if (string.Equals(appUserModelId, "chrome.exe", StringComparison.OrdinalIgnoreCase))
                return "Google Chrome";

            if (string.Equals(appUserModelId, "firefox.exe", StringComparison.OrdinalIgnoreCase))
                return "Mozilla Firefox";

            return appUserModelId;
        }

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}