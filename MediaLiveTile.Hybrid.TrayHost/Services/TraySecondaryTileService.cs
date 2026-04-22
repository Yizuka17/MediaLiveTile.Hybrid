using MediaLiveTile.Hybrid.TrayHost.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.StartScreen;

namespace MediaLiveTile.Hybrid.TrayHost.Services
{
    internal sealed class TraySecondaryTileService
    {
        public async Task<IReadOnlyList<ManagedSecondaryTileInfo>> GetManagedTilesAsync()
        {
            var allTiles = await SecondaryTile.FindAllAsync();
            var result = new List<ManagedSecondaryTileInfo>();

            foreach (var tile in allTiles)
            {
                var info = ParseManagedTile(tile);
                if (info is not null)
                {
                    result.Add(info);
                }
            }

            return result;
        }

        private ManagedSecondaryTileInfo? ParseManagedTile(SecondaryTile tile)
        {
            if (tile == null || string.IsNullOrWhiteSpace(tile.Arguments))
                return null;

            if (!tile.Arguments.StartsWith("mlt;", StringComparison.OrdinalIgnoreCase))
                return null;

            string? kindText = null;
            string? targetText = null;

            var parts = tile.Arguments.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith("kind=", StringComparison.OrdinalIgnoreCase))
                {
                    kindText = part.Substring("kind=".Length);
                }
                else if (part.StartsWith("target=", StringComparison.OrdinalIgnoreCase))
                {
                    targetText = part.Substring("target=".Length);
                }
            }

            if (!Enum.TryParse(kindText, true, out PinnedTileKind kind))
                return null;

            if (!int.TryParse(targetText, out int targetIndex))
                return null;

            return new ManagedSecondaryTileInfo
            {
                TileId = tile.TileId,
                Kind = kind,
                TargetIndex = targetIndex
            };
        }
    }
}