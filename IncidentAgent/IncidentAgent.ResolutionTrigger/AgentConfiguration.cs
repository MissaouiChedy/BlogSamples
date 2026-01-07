using System.ComponentModel.DataAnnotations;

namespace IncidentAgent.ResolutionTrigger;

public sealed class AgentConfiguration
{
    [Required]
    [Url]
    public required string Endpoint { get; init; }

    [Required]
    public required string DeploymentName { get; init; }

    [Required]
    [Url]
    public required string KnowledgeBaseMcpServerUrl { get; init; }
}
