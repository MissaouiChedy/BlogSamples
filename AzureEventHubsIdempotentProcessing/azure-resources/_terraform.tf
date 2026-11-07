terraform {
  backend "local" {}

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.13.0"
    }

    azapi = {
      source  = "azure/azapi"
      version = "~> 2.13.0"
    }
  }
}
