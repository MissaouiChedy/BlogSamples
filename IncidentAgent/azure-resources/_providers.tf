provider "azurerm" {
  subscription_id = var.subscription
  features {}
}

provider "azapi" {
  subscription_id = var.subscription
}