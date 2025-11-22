# Send Messages To Event Hub Topic JMeter Test Sample
A JMeter Test Plan sample to send events to an Azure Event Hubs topic.

This sample JMeter test, uses one of the [pnopjp/jmeter-plugins](https://github.com/pnopjp/jmeter-plugins) JMeter plugins: Eventhub Plugin.

When experimenting with event hub or for load testing purposes, it is interesting to send a large volume of events to an event hub topic. The purpose of the `SendMessagesToEventHubTopic.jmx` test is to provide a simple example of how can this be done. 

This example is discussed in the [Sending Events to Event Hubs With JMeter](http://blog.techdominator.com/article/sending-events-to-eventhubs-with-jmeter.html) blog post.

## Pre-Requisites
1. [Install a Java Runtime Environment](https://www.oracle.com/java/technologies/downloads/) version [recent enough](https://github.com/pnopjp/jmeter-plugins?tab=readme-ov-file#requirements) for the JMeter Eventhub Plugins (Version 17 at the time of this writing)
1. [Download](https://jmeter.apache.org/download_jmeter.cgi) and [Install JMeter Locally](https://www.simplilearn.com/tutorials/jmeter-tutorial/jmeter-installation)
1. [Install the EventHubs JMeter plugin from pnopjp/jmeter-plugins](https://github.com/pnopjp/jmeter-plugins?tab=readme-ov-file#how-to-install) 
1. [Create an Eventhubs Namespace and an Eventhub Topic](https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-create)

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
    - Adds a delay of 200 milliseconds between each request by default.

## How to Use
1. Open the JMeter application
1. Load the `SendMessagesToEventHubTopic.jmx` test plan via File > Open
![Open jmx test file in JMeter](doc-images/jmeter-file-open.png?raw=true)
1. Configure the user-defined variables with your Event Hub details
![JMeter User Defined Variables](doc-images/jmeter-test-user-defined-variables.png?raw=true)
1. Run the test plan to send messages to the Event Hub topic, results can be viewed in the 'View Result Tree' and 'Summary Report' Views
![JMeter Run Test Plan and View Results](doc-images/jmeter-launching-tests-viewing-results.png?raw=true)

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

For more information on configuring and running JMeter tests, refer to the [JMeter documentation](https://jmeter.apache.org/usermanual/index.html).

## Contributing

Please checkout [the contribution guidelines](../CONTRIBUTING.md) for contributing. 