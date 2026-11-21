namespace IdempotencyTechnique2
{
    public record OrderPlacedEvent(
        string OrderId, string CustomerEmail, decimal Total, DateTime PlacedAt);
}
