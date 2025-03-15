# Azure Event Hubs Raw AMQP Sample

A sample C# console app demonstrating how to use Raw AMQP connection to send and receive messages an event hubs partition.

This example is discussed in [Raw AMQP on Azure Event Hubs](http://blog.techdominator.com/article/raw-amqp-on-azure-event-hubs.html)

## Building and running the example

To run the sample you need:

 - [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
 - [Azure Subscription](https://azure.microsoft.com/en-us/pricing/purchase-options/azure-account)
 - [Terraform](https://developer.hashicorp.com/terraform/install?product_intent=terraform)

You can use the `azure-resources` terraform project to create the required resources.

## Applying the resources via terraform

To create the required resources using Terraform, navigate to the `azure-resources` folder. This folder contains the necessary Terraform configuration files to provision the Azure resources. Follow these steps:

1. Ensure you have [Terraform installed](https://learn.hashicorp.com/tutorials/terraform/install-cli).
1. Open a terminal and navigate to the `azure-resources` folder.
1. Initialize Terraform by running `terraform init`.
1. Review the configuration and make any necessary adjustments to the variables in the `_variables.tf` file.
1. Apply the Terraform configuration by running `terraform apply`.
1. Confirm the apply action when prompted.

Terraform will create the necessary Azure resources as defined in the configuration files. Ensure your Azure credentials are properly configured for authentication by executing an `az login` before running the Terraform commands.

## Acquiring SaS KEy for Event Hubs access

In this sample, to interact with Azure Event Hubs, you need a Shared Access Signature (SAS) key. 

SAS keys are generated based on the shared access policies defined for your Event Hub namespace or specific Event Hubs.

These policies determine the level of access (e.g., send, listen, or manage) granted to the key holder. Follow the steps below to acquire a SAS key for accessing your Event Hubs.

It is recommended to use forms of authentication that does not require providing explicit secrets which **SAS policies are not.**

Learn more about SAS Keys in the [official documentation.](https://learn.microsoft.com/en-us/azure/event-hubs/authenticate-shared-access-signature)