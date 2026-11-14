namespace IdempotencyTechnique1
{
    public record OrderPlaced(string OrderId, string CustomerEmail, decimal Total, DateTime PlacedAt);
}
