terraform {
  backend "local" {}

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.0.1"
    }
    local = {
      source  = "hashicorp/local"
      version = "~> 2.9.0"
    }
  }
}
