# Azure Event Hub Checkpoints and Rewinding

A sample C# console app demonstrating how to checkpoint and how to rewind the event stream with Azure Event hubs .NET SDK.

This example is discussed in [Azure Event Hubs Checkpoints & Rewinding](http://blog.techdominator.com/article/azure-event-hubs-checkpoints-%26-rewinding.html)

## Building and running the example

To run the sample you need:

 - [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
 - [Azure Subscription](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account)

You can use the `azure-resources.ps1` script to create the required resources.

## Running the JMeter Test

To run the JMeter test for sending messages to the Event Hub topic, follow these steps:

1. Download and install [Apache JMeter](https://jmeter.apache.org/download_jmeter.cgi).
2. Open JMeter and load the `SendMessagesToEventHubTopic.jmx` test plan.
3. Update the user-defined variables in the test plan with your Event Hub details:
    - `MessagesCount`: Number of messages to send.
    - `TopicName`: Name of your Event Hub topic.
    - `EventHubNamespace`: Your Event Hub namespace.
4. Start the test by clicking the green **Start** button in JMeter.
5. Monitor the test results in the **View Results Tree** and **Summary Report** listeners.

Ensure that your Azure credentials are properly configured for authentication by executing an `az login`