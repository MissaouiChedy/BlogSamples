namespace IncidentAgent.Web.Components.Configuration;

public class CosmosDbSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string TicketContainerId { get; set; } = string.Empty;
    public string ResolutionsContainerId { get; set; } = string.Empty;
    public string KnowledgeBaseContainerId { get; set; } = string.Empty;
}