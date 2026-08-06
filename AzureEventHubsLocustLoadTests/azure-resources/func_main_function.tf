resource "azurerm_service_plan" "main_function_plan" {
  name                = "asp-main-loadtest-${local.suffix}"
  resource_group_name = azurerm_resource_group.main_resource_group.name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "FC1"
}
resource "azurerm_function_app_flex_consumption" "main_function_app" {
  name                = "func-main-loadtest-${local.suffix}"
  resource_group_name = azurerm_resource_group.main_resource_group.name
  location            = var.location
  service_plan_id     = azurerm_service_plan.main_function_plan.id

  storage_container_type                         = "blobContainer"
  storage_container_endpoint                     = "${azurerm_storage_account.azure_function_storage_account.primary_blob_endpoint}${azurerm_storage_container.azure_function_webjobs_hosts_container.name}"
  storage_authentication_type                    = "StorageAccountConnectionString"
  storage_access_key                             = azurerm_storage_account.azure_function_storage_account.primary_access_key
  runtime_name                                   = "dotnet-isolated"
  runtime_version                                = "10.0"
  maximum_instance_count                         = 100
  instance_memory_in_mb                          = 2048
  webdeploy_publish_basic_authentication_enabled = true

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.main_func_identity.id]
  }

  app_settings = {
    AZURE_TENANT_ID                             = azurerm_user_assigned_identity.main_func_identity.tenant_id
    AZURE_CLIENT_ID                             = azurerm_user_assigned_identity.main_func_identity.client_id
    ENRICHED_EVENTS_CONTAINER                   = azurerm_storage_container.enriched_events_container.name
    STORAGE_ACCOUNT_URL                         = azurerm_storage_account.main_storage_account.primary_blob_endpoint
    EventHubConnection__fullyQualifiedNamespace = "${azurerm_eventhub_namespace.main_eventhub_namespace.name}.servicebus.windows.net"
    EventHubConnection__credential              = "managedidentity"
    EventHubConnection__clientId                = azurerm_user_assigned_identity.main_func_identity.client_id
  }

  site_config {
    application_insights_connection_string = azurerm_application_insights.main_application_insights.connection_string
    application_insights_key               = azurerm_application_insights.main_application_insights.instrumentation_key
  }
}

resource "azurerm_role_assignment" "func_main_role_assignment_reader_load_test_identity" {
  scope                = azurerm_function_app_flex_consumption.main_function_app.id
  role_definition_name = "Reader"
  principal_id         = azurerm_user_assigned_identity.main_load_test_identity.principal_id
}
