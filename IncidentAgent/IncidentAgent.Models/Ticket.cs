using Newtonsoft.Json;

namespace IncidentAgent.Models
{
    public class Ticket
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonProperty("_etag")]
        public string? ETag { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "ticket";

        [JsonProperty("Category")]
        public string Category { get; set; } = "default";
    }
}
