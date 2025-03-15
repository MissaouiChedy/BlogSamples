using Amqp;
using Amqp.Framing;
using System.Text;

string EventHubNamespace = "evh-test-raw-amqp";
string EventHubName = "main-topic";
string ConsumerGroup = "main-consumer";
string SasKeyName = "RootManageSharedAccessKey";
string SasKey = Uri.EscapeDataString("SAS_POLICY_KEY>");

string connectionString = $"amqps://{SasKeyName}:{SasKey}@{EventHubNamespace}.servicebus.windows.net";
string sendEntityPath = EventHubName;
string receiveEntityPath = $"{EventHubName}/ConsumerGroups/{ConsumerGroup}/Partitions/0";
int batchSize = 7;

/*
 * Open Connection, send batchSize of messages then close the connection 
 */
await SendMessageAsync(connectionString, sendEntityPath, batchSize);
Console.WriteLine($"Messages sent successfully.");
Console.WriteLine($"===========================");

/*
 * Open Connection, receive batchSize of messages then close the connection 
 */
await ReceiveMessageAsync(connectionString, receiveEntityPath, batchSize);

async Task SendMessageAsync(string connectionString,
    string entityPath, int batchSize)
{
    Connection connection = null;
    Session session = null;
    SenderLink sender = null;

    try
    {
        Address address = new(connectionString);
        connection = await Connection.Factory.CreateAsync(address);
        session = new Session(connection);

        var target = new Target { Address = entityPath };
        sender = new SenderLink(session, "sender-link", target.Address);

        for (int i = 0; i < batchSize; i++)
        {
            var message = $"Message {Guid.NewGuid()}";
            var messageBody = Encoding.UTF8.GetBytes(message);
            var amqpMessage = new Message
            {
                BodySection = new Data { Binary = messageBody }
            };
            await sender.SendAsync(amqpMessage);
            Console.WriteLine($"Sent message: {message}");
        }
    }
    finally
    {
        sender?.Close(TimeSpan.Zero);
        session?.Close(TimeSpan.Zero);
        connection?.Close(TimeSpan.Zero);
    }
}

async Task ReceiveMessageAsync(string connectionString,
    string entityPath, int batchSize)
{
    Connection connection = null;
    Session session = null;
    ReceiverLink receiver = null;

    try
    {
        Address address = new(connectionString);
        connection = await Connection.Factory.CreateAsync(address);
        session = new Session(connection);

        var source = new Source { Address = entityPath };
        receiver = new ReceiverLink(session, "receiver-link", source.Address);
        for (int i = 0; i < batchSize; i++)
        {
            var message = await receiver.ReceiveAsync();
            if (message != null)
            {
                var body = message.BodySection as Data;
                var messageBody = Encoding.UTF8.GetString(body.Binary);
                Console.WriteLine($"Received message: {messageBody}");
                receiver.Accept(message);
            }
        }
    }
    finally
    {
        receiver?.Close(TimeSpan.Zero);
        session?.Close(TimeSpan.Zero);
        connection?.Close(TimeSpan.Zero);
    }
}
