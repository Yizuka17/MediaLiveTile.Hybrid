using MediaLiveTile.Hybrid.TrayHost.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal sealed class TrayTileUpdateService
    {
        private const string DefaultImageUri = "ms-appx:///Assets/Square150x150Logo.png";
        private readonly TraySecondaryTileService _secondaryTileService = new TraySecondaryTileService();

        public async Task UpdateMainTileAsync(TrayMediaSelectionResult? result)
        {
            try
            {
                int smallTargetIndex = TrayTileSettingsService.GetSmallTileTargetIndex();
                int mediumTargetIndex = TrayTileSettingsService.GetMediumTileTargetIndex();
                int wideTargetIndex = TrayTileSettingsService.GetWideTileTargetIndex();
                int largeTargetIndex = TrayTileSettingsService.GetLargeTileTargetIndex();

                var updater = TileUpdateManager.CreateTileUpdaterForApplication();

                await UpdateTileAsync(
                    updater,
                    result,
                    smallTargetIndex,
                    mediumTargetIndex,
                    wideTargetIndex,
                    largeTargetIndex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Update main tile failed: " + ex);
            }
        }

        public async Task UpdateAllSecondaryTilesAsync(TrayMediaSelectionResult? result)
        {
            IReadOnlyList<ManagedSecondaryTileInfo> tiles;

            try
            {
                tiles = await _secondaryTileService.GetManagedTilesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Find secondary tiles failed: " + ex);
                return;
            }

            foreach (var tile in tiles)
            {
                try
                {
                    var updater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId);

                    await UpdateTileAsync(
                        updater,
                        result,
                        tile.TargetIndex,
                        tile.TargetIndex,
                        tile.TargetIndex,
                        tile.TargetIndex);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Update secondary tile failed: " + tile.TileId + " | " + ex);
                }
            }
        }

        private async Task UpdateTileAsync(
            TileUpdater updater,
            TrayMediaSelectionResult? result,
            int smallTargetIndex,
            int mediumTargetIndex,
            int wideTargetIndex,
            int largeTargetIndex)
        {
            var allSessions = result?.AllSessions;

            long cacheVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var smallItem = await CreateTileItemAsync(ResolveMedia(allSessions, smallTargetIndex), cacheVersion);
            var mediumItem = await CreateTileItemAsync(ResolveMedia(allSessions, mediumTargetIndex), cacheVersion);
            var wideItem = await CreateTileItemAsync(ResolveMedia(allSessions, wideTargetIndex), cacheVersion);
            var largeItem = await CreateTileItemAsync(ResolveMedia(allSessions, largeTargetIndex), cacheVersion);

            var coverXml = BuildCoverTileXml(smallItem, mediumItem, wideItem, largeItem);
            var infoXml = BuildInfoTileXml(smallItem, mediumItem, wideItem, largeItem);

            var coverDoc = new XmlDocument();
            coverDoc.LoadXml(coverXml);

            var infoDoc = new XmlDocument();
            infoDoc.LoadXml(infoXml);

            updater.Clear();

            updater.EnableNotificationQueue(false);
            updater.EnableNotificationQueueForSquare150x150(true);
            updater.EnableNotificationQueueForSquare310x310(true);
            updater.EnableNotificationQueueForWide310x150(false);

            updater.Update(CreateTileNotification(coverDoc));
            updater.Update(CreateTileNotification(infoDoc));
        }

        private TileNotification CreateTileNotification(XmlDocument document)
        {
            return new TileNotification(document)
            {
                ExpirationTime = DateTimeOffset.Now.AddDays(1)
            };
        }

        private TrayMediaSessionInfo? ResolveMedia(IReadOnlyList<TrayMediaSessionInfo>? allSessions, int index)
        {
            if (allSessions == null || allSessions.Count == 0)
                return null;

            if (index < 0)
                index = 0;

            if (index >= allSessions.Count)
                return null;

            return allSessions[index];
        }

        private Task<TileItem> CreateTileItemAsync(TrayMediaSessionInfo? media, long cacheVersion)
        {
            return Task.FromResult(new TileItem
            {
                ImageUri = GetImageUri(media),
                ImageCacheVersion = cacheVersion,
                Title = GetDisplayTitle(media),
                Artist = GetArtist(media),
                Source = GetSource(media)
            });
        }

        private string GetImageUri(TrayMediaSessionInfo? media)
        {
            if (!string.IsNullOrWhiteSpace(media?.EffectiveImageUri))
                return media.EffectiveImageUri!;

            return DefaultImageUri;
        }

        private string BuildCoverTileXml(
            TileItem small,
            TileItem medium,
            TileItem wide,
            TileItem large)
        {
            return
$@"<tile>
    <visual version='3'>
        {BuildSmallCoverBinding(small)}
        {BuildMediumCoverBinding(medium)}
        {BuildWideStaticBinding(wide)}
        {BuildLargeCoverBinding(large)}
    </visual>
</tile>";
        }

        private string BuildInfoTileXml(
            TileItem small,
            TileItem medium,
            TileItem wide,
            TileItem large)
        {
            return
$@"<tile>
    <visual version='3'>
        {BuildSmallCoverBinding(small)}
        {BuildMediumInfoBinding(medium)}
        {BuildWideStaticBinding(wide)}
        {BuildLargeInfoBinding(large)}
    </visual>
</tile>";
        }

        private string BuildSmallCoverBinding(TileItem item)
        {
            return
$@"<binding template='TileSmall' branding='none'>
    <image src='{EscapeXml(GetVersionedImageUri(item))}' hint-removeMargin='true'/>
</binding>";
        }

        private string BuildMediumCoverBinding(TileItem item)
        {
            return
$@"<binding template='TileMedium' branding='none'>
    <image src='{EscapeXml(GetVersionedImageUri(item))}' placement='background'/>
</binding>";
        }

        private string BuildMediumInfoBinding(TileItem item)
        {
            return
$@"<binding template='TileMedium' branding='none'>
    <text hint-style='subtitle' hint-wrap='true' hint-maxLines='2'>{EscapeXml(item.Title)}</text>
    <text hint-style='body' hint-wrap='true' hint-maxLines='2'>{EscapeXml(item.Artist)}</text>
    <text hint-style='captionSubtle' hint-wrap='true' hint-maxLines='1'>{EscapeXml(item.Source)}</text>
</binding>";
        }

        private string BuildWideStaticBinding(TileItem item)
        {
            return
$@"<binding template='TileWide' branding='none'>
    <group>
        <subgroup hint-weight='50'>
            <image src='{EscapeXml(GetVersionedImageUri(item))}' hint-removeMargin='true'/>
        </subgroup>
        <subgroup hint-weight='50'>
            <text hint-style='subtitle' hint-wrap='true' hint-maxLines='2'>{EscapeXml(item.Title)}</text>
            <text hint-style='body' hint-wrap='true' hint-maxLines='2'>{EscapeXml(item.Artist)}</text>
            <text hint-style='captionSubtle' hint-wrap='true' hint-maxLines='1'>{EscapeXml(item.Source)}</text>
        </subgroup>
    </group>
</binding>";
        }

        private string BuildLargeCoverBinding(TileItem item)
        {
            return
$@"<binding template='TileLarge' branding='none'>
    <image src='{EscapeXml(GetVersionedImageUri(item))}' placement='background'/>
</binding>";
        }

        private string BuildLargeInfoBinding(TileItem item)
        {
            return
$@"<binding template='TileLarge' branding='none'>
    <text hint-style='title' hint-wrap='true' hint-maxLines='3'>{EscapeXml(item.Title)}</text>
    <text hint-style='body' hint-wrap='true' hint-maxLines='2'>{EscapeXml(item.Artist)}</text>
    <text hint-style='captionSubtle' hint-wrap='true' hint-maxLines='1'>{EscapeXml(item.Source)}</text>
</binding>";
        }

        private string GetDisplayTitle(TrayMediaSessionInfo? media)
        {
            if (!string.IsNullOrWhiteSpace(media?.Title) &&
                !string.Equals(media.Title, "无标题", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(media.Title, "读取失败", StringComparison.OrdinalIgnoreCase))
            {
                return media.Title;
            }

            return "当前无媒体";
        }

        private string GetArtist(TrayMediaSessionInfo? media)
        {
            if (media == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(media.Artist) &&
                !string.Equals(media.Artist, "无艺人", StringComparison.OrdinalIgnoreCase))
            {
                return media.Artist;
            }

            return string.Empty;
        }

        private string GetSource(TrayMediaSessionInfo? media)
        {
            return string.IsNullOrWhiteSpace(media?.SourceDisplayName)
                ? string.Empty
                : media.SourceDisplayName;
        }

        private string GetVersionedImageUri(TileItem item)
        {
            if (string.IsNullOrWhiteSpace(item.ImageUri))
                return DefaultImageUri;

            string separator = item.ImageUri.Contains("?") ? "&" : "?";
            return item.ImageUri + separator + "v=" + item.ImageCacheVersion.ToString();
        }

        private string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private sealed class TileItem
        {
            public string ImageUri { get; set; } = string.Empty;
            public long ImageCacheVersion { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Artist { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
        }
    }
}
