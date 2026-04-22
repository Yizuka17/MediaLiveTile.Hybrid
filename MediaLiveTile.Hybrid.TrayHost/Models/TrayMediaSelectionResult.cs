using System.Collections.Generic;

namespace MediaLiveTile.Hybrid.TrayHost.Models
{
    internal sealed class TrayMediaSelectionResult
    {
        public IReadOnlyList<TrayMediaSessionInfo> AllSessions { get; set; } =
            new List<TrayMediaSessionInfo>();

        public TrayMediaSessionInfo? PrimaryMedia { get; set; }

        public TrayMediaSessionInfo? SecondaryMedia { get; set; }

        public int Count => AllSessions?.Count ?? 0;
    }
}