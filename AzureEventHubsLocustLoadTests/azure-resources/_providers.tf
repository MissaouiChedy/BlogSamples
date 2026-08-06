provider "azurerm" {
  subscription_id = var.subscription
  resource_providers_to_register = [
    "Microsoft.Authorization",
    "Microsoft.EventHub",
    "Microsoft.Insights",
    "Microsoft.LoadTestService",
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
