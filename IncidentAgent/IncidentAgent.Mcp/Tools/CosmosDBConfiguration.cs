namespace IncidentAgent.Mcp.Tools
{
    public class CosmosDBConfiguration
    {
        public required string Endpoint { get; set; }
        public required string DatabaseId { get; set; }
        public required string ContainerId { get; set; }
    }
}
