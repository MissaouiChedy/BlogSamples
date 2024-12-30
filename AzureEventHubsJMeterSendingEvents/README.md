# Send Messages To Event Hub Topic JMeter Test Sample
A JMeter Test Plan to send events to an Azure Event Hubs topic.

This example is discussed in [Sending Events to Event Hubs With JMeter.](http://blog.techdominator.com/article/sending-events-to-eventhubs-with-jmeter.html)

## Test Plan Structure

The test plan consists of the following components:

1. **User Defined Variables**:
    - `EventHubNamespacePrimaryKey`: The primary key for the Event Hub namespace.
    - `MessagesCount`: The number of messages to send.
    - `TopicName`: The name of the Event Hub topic.
    - `EventHubSharedAccessPolicyName`: The shared access policy name.
    - `EventHubNamespace`: The namespace of the Event Hub.

2. **Thread Groups**:
    - There are 10 thread groups, each configured to send messages to a different partition of the Event Hub.
    - Each thread group contains a loop controller that iterates based on the `MessagesCount` variable.
    - Each thread group includes an Azure Event Hubs Sampler configured with the necessary properties to send messages.

3. **Azure Event Hubs Sampler**:
    - Configured with the namespace, authentication type, shared access key name, shared access key, event hub name, partition type, and partition value.
    - Each sampler sends a message with a unique ID and content to the specified partition.

4. **Result Collectors**:
    - `View Results Tree`: Displays detailed results of each sample.
    - `Summary Report`: Provides a summary of the test results.

5. **Constant Timer**:
    - Adds a delay of 200 milliseconds between each request.

## How to Use
1. Open the JMeter application.
2. Load the `SendMessagesToEventHubTopic.jmx` test plan.
3. Configure the user-defined variables with your Event Hub details.
4. Run the test plan to send messages to the Event Hub topic.

## Example Message
Each message sent to the Event Hub has the following structure:

```json
{
  "Id": "${__UUID}",
  "LocationId": "1",
  "Content": "KABLAM"
}
```

The `Id` field is a unique identifier generated for each message.

## Notes
- Ensure that the Event Hub namespace, shared access policy, and primary key are correctly configured.
- Adjust the `MessagesCount` variable to control the number of messages sent by each thread group.

For more information on configuring and running JMeter tests, refer to the [JMeter documentation](https://jmeter.apache.org/usermanual/index.html).