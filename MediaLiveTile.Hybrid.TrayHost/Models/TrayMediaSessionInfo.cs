namespace MediaLiveTile.Hybrid.TrayHost.Models
{
    internal sealed class TrayMediaSessionInfo
    {
        public int Order { get; set; }

        public string SourceAppUserModelId { get; set; } = string.Empty;

        public string SourceDisplayName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Artist { get; set; } = string.Empty;

        public string AlbumTitle { get; set; } = string.Empty;

        public string PlaybackStatus { get; set; } = string.Empty;

        public bool IsPlaying { get; set; }

        public bool IsCurrentSession { get; set; }

        public bool IsMusicPreferred { get; set; }

        public int InfoCompletenessScore { get; set; }

        public string Role { get; set; } = string.Empty;

        public bool HasThumbnail => !string.IsNullOrWhiteSpace(ThumbnailLocalUri);

        public string? ThumbnailLocalUri { get; set; }

        public string? AppIconLocalUri { get; set; }

        public string? EffectiveImageUri =>
            !string.IsNullOrWhiteSpace(ThumbnailLocalUri)
                ? ThumbnailLocalUri
                : AppIconLocalUri;
    }
}