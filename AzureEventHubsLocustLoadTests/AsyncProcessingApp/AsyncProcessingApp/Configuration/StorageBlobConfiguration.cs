using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace AsyncProcessingApp.Configuration;

/*
 * Strongly typed configuration class for the storage blob configuration.
 */
public sealed class StorageBlobConfiguration
{
    [ConfigurationKeyName("STORAGE_ACCOUNT_URL")]
    [Required(ErrorMessage = "Missing required configuration value STORAGE_ACCOUNT_URL.")]
    public string? StorageAccountUrl { get; set; }

    [ConfigurationKeyName("ENRICHED_EVENTS_CONTAINER")]
    public string EnrichedEventsContainer { get; set; } = "enriched-events";
}
