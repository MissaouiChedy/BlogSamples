resource "azurerm_service_plan" "main_service_plan" {
  location            = var.location
  name                = "ASP-mainserviceplan-f1e6"
  os_type             = "Windows"
  resource_group_name = azurerm_resource_group.main_resource_group.name
  sku_name            = "Y1"
}

resource "azurerm_windows_function_app" "main_azure_function_app" {
  app_settings = {
    WEBSITE_RUN_FROM_PACKAGE               = "0"
    FUNCTIONS_WORKER_RUNTIME               = "dotnet-isolated"
    WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED = "1"
    APPINSIGHTS_INSTRUMENTATIONKEY         = azurerm_application_insights.main_application_insights.instrumentation_key
    APPLICATIONINSIGHTS_CONNECTION_STRING  = azurerm_application_insights.main_application_insights.connection_string
    AZURE_TENANT_ID                        = azurerm_user_assigned_identity.main_func_identity.tenant_id
    AZURE_CLIENT_ID                        = azurerm_user_assigned_identity.main_func_identity.client_id
    REDIS_URL                              = "${azurerm_managed_redis.main_redis_cache.hostname}:${azurerm_managed_redis.main_redis_cache.default_database[0].port}"
    STORAGE_ACCOUNT_URL                    = azurerm_storage_account.main_storage_account.primary_blob_endpoint
  }

  builtin_logging_enabled                  = true
  client_certificate_mode                  = "Required"
  ftp_publish_basic_authentication_enabled = false
  location                                 = var.location
  name                                     = "mainfuncapplatencyb74b"
  resource_group_name                      = azurerm_resource_group.main_resource_group.name
  service_plan_id                          = azurerm_service_plan.main_service_plan.id
  storage_account_access_key               = azurerm_storage_account.azure_function_storage_account.primary_access_key
  storage_account_name                     = azurerm_storage_account.azure_function_storage_account.name
  https_only                               = true
  tags = {
    "hidden-link: /app-insights-resource-id" = azurerm_application_insights.main_application_insights.id
  }

  webdeploy_publish_basic_authentication_enabled = false

  site_config {
    application_insights_connection_string = azurerm_application_insights.main_application_insights.connection_string
    ftps_state                             = "FtpsOnly"
    use_32_bit_worker                      = false
    cors {
      allowed_origins = ["https://portal.azure.com"]
    }

    application_stack {
      dotnet_version              = "v10.0"
      use_dotnet_isolated_runtime = true
    }
  }

  identity {
    type = "UserAssigned"
    identity_ids = [
      azurerm_user_assigned_identity.main_func_identity.id,
    ]
  }
}
