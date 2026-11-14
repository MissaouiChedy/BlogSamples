namespace IdempotencyTechnique1
{
    /*
     * Fake Email Sender stands in for an email provider (such as M365):
     *   The side effect can't be queried afterwards ("was this email already sent?")
     *   The side effect can't share a transaction with the processed events store
     *   Each sent email is printed to the console
     */

    public class FakeEmailSender
    {
        public Task SendOrderConfirmationAsync(OrderPlaced order)
        {
            Console.WriteLine($"RECEIVER: Email sent to {order.CustomerEmail}: order {order.OrderId} confirmed, total {order.Total:F2}");

            return Task.CompletedTask;
        }
    }
}
