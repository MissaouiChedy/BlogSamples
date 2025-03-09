data "azurerm_subscription" "main_subscription" {}

resource "azurerm_resource_group" "main_resource_group" {
  name     = "rg-test-eventhub-parition-receiver"
  location = var.location
}
