using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaLiveTile.Hybrid.Uwp.Models
{
    [DataContract]
    public sealed class UwpStateSnapshot
    {
        [DataMember(Order = 1)]
        public string StatusText { get; set; }

        [DataMember(Order = 2)]
        public bool IsMonitoringPaused { get; set; }

        [DataMember(Order = 3)]
        public bool HasLastRefreshTime { get; set; }

        [DataMember(Order = 4)]
        public long LastRefreshUnixTimeMilliseconds { get; set; }

        [DataMember(Order = 5)]
        public UwpStateSessionItem PrimaryMedia { get; set; }

        [DataMember(Order = 6)]
        public UwpStateSessionItem SecondaryMedia { get; set; }

        [DataMember(Order = 7)]
        public List<UwpStateSessionItem> Sessions { get; set; }
    }
}