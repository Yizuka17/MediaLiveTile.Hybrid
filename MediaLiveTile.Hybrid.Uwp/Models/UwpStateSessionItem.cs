using System.Runtime.Serialization;

namespace MediaLiveTile.Hybrid.Uwp.Models
{
    [DataContract]
    public sealed class UwpStateSessionItem
    {
        [DataMember(Order = 1)]
        public int Order { get; set; }

        [DataMember(Order = 2)]
        public string Role { get; set; }

        [DataMember(Order = 3)]
        public string SourceDisplayName { get; set; }

        [DataMember(Order = 4)]
        public string SourceAppUserModelId { get; set; }

        [DataMember(Order = 5)]
        public string Title { get; set; }

        [DataMember(Order = 6)]
        public string Artist { get; set; }

        [DataMember(Order = 7)]
        public string AlbumTitle { get; set; }

        [DataMember(Order = 8)]
        public string PlaybackStatus { get; set; }

        [DataMember(Order = 9)]
        public string ImageUri { get; set; }
    }
}