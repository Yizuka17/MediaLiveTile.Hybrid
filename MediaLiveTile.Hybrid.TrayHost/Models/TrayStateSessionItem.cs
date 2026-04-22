using System.Runtime.Serialization;

namespace MediaLiveTile.Hybrid.TrayHost.Models
{
    [DataContract]
    internal sealed class TrayStateSessionItem
    {
        [DataMember(Order = 1)]
        public int Order { get; set; }

        [DataMember(Order = 2)]
        public string Role { get; set; } = string.Empty;

        [DataMember(Order = 3)]
        public string SourceDisplayName { get; set; } = string.Empty;

        [DataMember(Order = 4)]
        public string SourceAppUserModelId { get; set; } = string.Empty;

        [DataMember(Order = 5)]
        public string Title { get; set; } = string.Empty;

        [DataMember(Order = 6)]
        public string Artist { get; set; } = string.Empty;

        [DataMember(Order = 7)]
        public string AlbumTitle { get; set; } = string.Empty;

        [DataMember(Order = 8)]
        public string PlaybackStatus { get; set; } = string.Empty;

        [DataMember(Order = 9)]
        public string ImageUri { get; set; } = string.Empty;
    }
}