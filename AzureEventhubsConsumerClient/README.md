# Azure Event Hub Consumer Client Sample

A sample C# console app demonstrating how to use the `EventHubConsumerClient` to consume events from a partition.

This example is discussed in [Azure Event Hubs Consumer Client](http://blog.techdominator.com/article/azure-event-hubs-consumer-client.html)

## Building and running the example

To run the sample you need:

 - [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
 - [Azure Subscription](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account)

You can use the `azure-resources.ps1` script to create the required resources.

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

## Getting an Access Token for Redis

To generate an access token for connecting to Redis, you can use the `Get-EntraToken.ps1` script. 

This script simplifies the process of obtaining an Azure Active Directory (AAD) token, which is necessary for authenticating with Azure Cache for Redis.

By running the script, you can retrieve a token that can be used with tools like [Redis Insights](https://redis.io/insight/) to manage your Redis instance. 

Ensure you have logged in to your Azure account using `az login` before executing the script.
