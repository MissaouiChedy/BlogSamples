using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncidentAgent.Models
{
    public class KnowledgeBaseEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Title { get; set; }

        public string Issue { get; set; }

        public string Solution { get; set; }

        public string Discussion { get; set; }

        [JsonProperty("Category")]
        public string Category { get; set; } = "default";

        [JsonProperty("_etag")]
        public string? ETag { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "knowledgeBaseEntry";
    }
}
