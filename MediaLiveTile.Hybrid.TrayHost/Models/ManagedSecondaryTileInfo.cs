namespace MediaLiveTile.Hybrid.TrayHost.Models
{
    internal sealed class ManagedSecondaryTileInfo
    {
        public string TileId { get; set; } = string.Empty;

        public PinnedTileKind Kind { get; set; }

        public int TargetIndex { get; set; }
    }
}