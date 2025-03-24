data "azurerm_subscription" "main_subscription" {}

resource "azurerm_resource_group" "main_resource_group" {
  name     = "rg-test-checkpoint-blob-latency"
  location = var.location
}
