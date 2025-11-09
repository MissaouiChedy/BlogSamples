data "azurerm_subscription" "main_subscription" {}

data "azurerm_resource_group" "main_resource_group" {
  name = var.resource_group_name
}
