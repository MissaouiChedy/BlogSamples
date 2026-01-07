using Newtonsoft.Json;

namespace IncidentAgent.Models
{
    public class Resolution
    {
        /* id field naming should be starting with lowercase to work with Azure Function Cosmos 
         * DB output binding
         */

        [JsonProperty("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("ticketId")]
        public string TicketId { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string[] Steps { get; set; } = Array.Empty<string>();

        [JsonProperty("type")]
        public string Type { get; set; } = "resolution";

        [JsonProperty("Category")]
        public string Category { get; set; } = "default";
    }
}