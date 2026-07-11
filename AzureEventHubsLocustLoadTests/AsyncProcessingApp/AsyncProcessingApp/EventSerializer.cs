using System.Text.Json;
using AsyncProcessingApp.Models;

namespace AsyncProcessingApp;

public static class EventSerializer
{
    public static MainTopicEvent DeserializeMainTopicEvent(string eventBody)
    {
        var payload = JsonSerializer.Deserialize<MainTopicEvent>(eventBody);
        return payload ?? throw new JsonException("Deserialized main topic event payload was null.");
    }

    public static string SerializeProcessedEvents(EnrichedMainTopicEvent enrichedEvent)
    {
        return JsonSerializer.Serialize(enrichedEvent);
    }
}
