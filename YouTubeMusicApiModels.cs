using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YouTubeMusicWidget
{
    public class State
    {
        [JsonPropertyName("player")]
        public Player Player { get; set; }

        [JsonPropertyName("video")]
        public Video Video { get; set; }

        [JsonPropertyName("playlistId")]
        public string PlaylistId { get; set; }
    }

    public class Player
    {
        [JsonPropertyName("trackState")]
        public int TrackState { get; set; }

        [JsonPropertyName("videoProgress")]
        public double VideoProgress { get; set; }

        [JsonPropertyName("volume")]
        public int Volume { get; set; }

        [JsonPropertyName("adPlaying")]
        public bool AdPlaying { get; set; }

        [JsonPropertyName("queue")]
        public Queue Queue { get; set; }

        // Added the missing Shuffle property
        [JsonPropertyName("shuffle")]
        public bool Shuffle { get; set; }
    }

    public class Queue
    {
        [JsonPropertyName("autoplay")]
        public bool Autoplay { get; set; }

        [JsonPropertyName("items")]
        public List<QueueItem> Items { get; set; }

        [JsonPropertyName("automixItems")]
        public List<QueueItem> AutomixItems { get; set; }

        [JsonPropertyName("isGenerating")]
        public bool IsGenerating { get; set; }

        [JsonPropertyName("isInfinite")]
        public bool IsInfinite { get; set; }

        [JsonPropertyName("repeatMode")]
        public int RepeatMode { get; set; }

        [JsonPropertyName("selectedItemIndex")]
        public int SelectedItemIndex { get; set; }
    }

    public class QueueItem
    {
        [JsonPropertyName("thumbnails")]
        public List<Thumbnail> Thumbnails { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        [JsonPropertyName("selected")]
        public bool Selected { get; set; }

        [JsonPropertyName("videoId")]
        public string VideoId { get; set; }

        [JsonPropertyName("counterparts")]
        public List<QueueItem> Counterparts { get; set; }
    }

    public class Thumbnail
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class Video
    {
        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("channelId")]
        public string ChannelId { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("album")]
        public string Album { get; set; }

        [JsonPropertyName("albumId")]
        public string AlbumId { get; set; }

        [JsonPropertyName("likeStatus")]
        public int? LikeStatus { get; set; }

        [JsonPropertyName("thumbnails")]
        public List<Thumbnail> Thumbnails { get; set; }

        [JsonPropertyName("durationSeconds")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("isLive")]
        public bool? IsLive { get; set; }

        [JsonPropertyName("videoType")]
        public int? VideoType { get; set; }

        [JsonPropertyName("metadataFilled")]
        public bool? MetadataFilled { get; set; }
    }

    public class Playlist
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }
}
