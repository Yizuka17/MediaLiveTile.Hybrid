using MediaLiveTile.Hybrid.Uwp.Models;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.StartScreen;

namespace MediaLiveTile.Hybrid.Uwp.Services
{
    internal sealed class UwpSecondaryTileService
    {
        private const string TileIdPrefix = "MediaLiveTile.Hybrid.Pinned.";

        private const string Square150LogoUri = "ms-appx:///Assets/Square150x150Logo.png";
        private const string Square71LogoUri = "ms-appx:///Assets/Square71x71Logo.png";
        private const string Wide310LogoUri = "ms-appx:///Assets/Wide310x150Logo.png";
        private const string Square310LogoUri = "ms-appx:///Assets/Square310x310Logo.png";

        public string CreateTileId(PinnedTileKind kind, int targetIndex)
        {
            return string.Format(
                "{0}{1}.{2}",
                TileIdPrefix,
                kind.ToString().ToLowerInvariant(),
                targetIndex);
        }

        public bool Exists(PinnedTileKind kind, int targetIndex)
        {
            return SecondaryTile.Exists(CreateTileId(kind, targetIndex));
        }

        public async Task<bool> RequestCreateAsync(PinnedTileKind kind, int targetIndex, string targetDisplayText)
        {
            string tileId = CreateTileId(kind, targetIndex);
            string displayName = string.Format("{0} - {1}", GetKindDisplayName(kind), targetDisplayText);
            string arguments = string.Format("mlt;kind={0};target={1}", kind.ToString().ToLowerInvariant(), targetIndex);

            var tile = new SecondaryTile(tileId)
            {
                DisplayName = displayName,
                Arguments = arguments,
                RoamingEnabled = false
            };

            tile.VisualElements.BackgroundColor = Colors.Transparent;
            tile.VisualElements.Square150x150Logo = new Uri(Square150LogoUri);
            tile.VisualElements.Square71x71Logo = new Uri(Square71LogoUri);
            tile.VisualElements.Wide310x150Logo = new Uri(Wide310LogoUri);
            tile.VisualElements.Square310x310Logo = new Uri(Square310LogoUri);

            tile.VisualElements.ShowNameOnSquare150x150Logo = false;
            tile.VisualElements.ShowNameOnWide310x150Logo = false;
            tile.VisualElements.ShowNameOnSquare310x310Logo = false;

            return await tile.RequestCreateAsync();
        }

        private string GetKindDisplayName(PinnedTileKind kind)
        {
            switch (kind)
            {
                case PinnedTileKind.Small:
                    return "小磁贴";
                case PinnedTileKind.Medium:
                    return "中磁贴";
                case PinnedTileKind.Wide:
                    return "宽磁贴";
                case PinnedTileKind.Large:
                    return "大磁贴";
                default:
                    return "磁贴";
            }
        }
    }
}