data "azurerm_subscription" "main_subscription" {}

resource "azurerm_resource_group" "main_resource_group" {
  name     = "rg-test-azure-event-hubs-idempotent-processing"
  location = var.location
}
