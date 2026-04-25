using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaLiveTile.Hybrid.Shared.Models
{
    [DataContract]
    public sealed class SharedStateSnapshot
    {
        [DataMember(Order = 1)]
        public string StatusText { get; set; } = string.Empty;

        [DataMember(Order = 2)]
        public bool IsMonitoringPaused { get; set; }

        [DataMember(Order = 3)]
        public bool HasLastRefreshTime { get; set; }

        [DataMember(Order = 4)]
        public long LastRefreshUnixTimeMilliseconds { get; set; }

        [DataMember(Order = 5)]
        public SharedStateSessionItem? PrimaryMedia { get; set; }

        [DataMember(Order = 6)]
        public SharedStateSessionItem? SecondaryMedia { get; set; }

        [DataMember(Order = 7)]
        public List<SharedStateSessionItem> Sessions { get; set; } = new List<SharedStateSessionItem>();
    }
}
