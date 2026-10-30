provider "azurerm" {
  subscription_id = var.subscription
  resource_providers_to_register = [
    "Microsoft.Authorization",
    "Microsoft.CognitiveServices",
    "Microsoft.DocumentDB",
    "Microsoft.Insights",
    "Microsoft.ManagedIdentity",
    "Microsoft.OperationalInsights",
    "Microsoft.Resources",
    "Microsoft.Storage",
    "Microsoft.Web",
  ]

  features {
    enhanced_validation {
      preflight_enabled  = true
      resource_providers = true
      locations          = true
    }

    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

provider "azapi" {
  subscription_id = var.subscription
}
