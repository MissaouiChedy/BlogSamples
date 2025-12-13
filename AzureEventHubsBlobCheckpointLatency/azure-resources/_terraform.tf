terraform {
  backend "local" {}

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.54.0"
    }

    azapi = {
      source  = "azure/azapi"
      version = "~> 2.7.0"
    }
  }
}
