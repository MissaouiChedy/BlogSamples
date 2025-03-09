# Azure Event Hubs Partition Receiver Sample

A sample C# console app demonstrating how to use the `PartitionReceiver` to consume events from an event hubs partition.

This example is discussed in [Azure Event Hubs Partition Receiver](http://blog.techdominator.com/article/azure-event-hubs-partition-receiver.html)

## Building and running the example

To run the sample you need:

 - [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
 - [Azure Subscription](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account)
 - [Terraform](https://developer.hashicorp.com/terraform/install?product_intent=terraform)

You can use the `azure-resources` terraform project to create the required resources.

## Running the JMeter Test

To run the JMeter test for sending messages to the Event Hub topic, follow these steps:

1. Download and install [Apache JMeter](https://jmeter.apache.org/download_jmeter.cgi).
1. Download and install the [Eventhubs JMeter Plugin.](https://github.com/pnopjp/jmeter-plugins)
1. Open JMeter and load the `SendMessagesToEventHubTopic.jmx` test plan.
1. Update the user-defined variables in the test plan with your Event Hub details:
    - `MessagesCount`: Number of messages to send.
    - `TopicName`: Name of your Event Hub topic.
    - `EventHubNamespace`: Your Event Hub namespace.
1. Start the test by clicking the green **Start** button in JMeter.
1. Monitor the test results in the **View Results Tree** and **Summary Report** listeners.

Ensure that your Azure credentials are properly configured for authentication by executing an `az login`

## Applying the resources via terraform

To create the required resources using Terraform, navigate to the `azure-resources` folder. This folder contains the necessary Terraform configuration files to provision the Azure resources. Follow these steps:

1. Ensure you have [Terraform installed](https://learn.hashicorp.com/tutorials/terraform/install-cli).
1. Open a terminal and navigate to the `azure-resources` folder.
1. Initialize Terraform by running `terraform init`.
1. Review the configuration and make any necessary adjustments to the variables in the `_variables.tf` file.
1. Apply the Terraform configuration by running `terraform apply`.
1. Confirm the apply action when prompted.

Terraform will create the necessary Azure resources as defined in the configuration files. Ensure your Azure credentials are properly configured for authentication by executing an `az login` before running the Terraform commands.

## Setting Startup Project in Visual Studio

This solution contains two projects:
- `AzureEventHubsPartitionReceiver`
- `AzureEventHubsPartitionReceiver.MultipleReceivers`


When working on a solution with multiple projects in Visual Studio, you need to specify which project should be run when you start debugging. This is known as setting the startup project. Follow these steps to set the startup project:

1. **Open Solution Explorer**: You can open the Solution Explorer by selecting `View > Solution Explorer` from the menu bar or by pressing `Ctrl+Alt+L`.

2. **Locate the Project**: In the Solution Explorer, locate the project that you want to set as the startup project.

3. **Set as Startup Project**:
   - **Right-click on the Project**: Right-click on the project name.
   - **Select 'Set as Startup Project'**: From the context menu, select `Set as Startup Project`.

4. **Verify the Startup Project**: The project name should now appear in bold in the Solution Explorer, indicating that it is the startup project.
