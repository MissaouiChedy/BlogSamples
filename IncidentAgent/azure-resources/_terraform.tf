terraform {
  backend "local" {}
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.7.0"
    }
    azapi = {
      source  = "azure/azapi"
      version = "~> 2.13.0"
    }
  }
}
