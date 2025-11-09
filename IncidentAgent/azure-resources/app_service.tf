resource "azurerm_service_plan" "app_plan" {
  name                = "plan-ticket-classification"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  os_type             = "Windows"
  sku_name            = "D1"
}

resource "azurerm_windows_web_app" "app" {
  name                = "app-ticket-classification-chk-${local.resources_suffix}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  service_plan_id     = azurerm_service_plan.app_plan.id

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.openai_identity.id]
  }
  site_config {
    always_on = false
    application_stack {
      dotnet_version = "v10.0"
    }

    cors {
      allowed_origins     = ["*"]
      support_credentials = false
    }

    websockets_enabled = true
  }

  client_affinity_enabled = true

  app_settings = {
    ASPNETCORE_ENVIRONMENT      = "Development"
    WEBSITE_RUN_FROM_PACKAGE    = "1"
    CosmosDb__Endpoint          = azurerm_cosmosdb_account.ticket_db.endpoint
    CosmosDb__DatabaseId        = azurerm_cosmosdb_sql_database.tickets.name
    CosmosDb__ContainerId       = azurerm_cosmosdb_sql_container.ticket_container.name
    AzureOpenAI__Endpoint       = local.foundry_endpoint
    AzureOpenAI__DeploymentName = var.model_deployment_name
    AZURE_TENANT_ID             = azurerm_user_assigned_identity.openai_identity.tenant_id
    AZURE_CLIENT_ID             = azurerm_user_assigned_identity.openai_identity.client_id
  }
}
