using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaLiveTile.Hybrid.TrayHost.Models
{
    [DataContract]
    internal sealed class TrayStateSnapshot
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
        public TrayStateSessionItem PrimaryMedia { get; set; }

        [DataMember(Order = 6)]
        public TrayStateSessionItem SecondaryMedia { get; set; }

        [DataMember(Order = 7)]
        public List<TrayStateSessionItem> Sessions { get; set; } = new List<TrayStateSessionItem>();
    }
}