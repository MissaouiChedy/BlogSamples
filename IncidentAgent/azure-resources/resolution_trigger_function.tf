# Function App
# Assign Managed Identity to Function App
# Env var injection to access Open AI service and Cosmos DB
# 

resource "azurerm_service_plan" "main_service_plan" {
  location            = var.location
  name                = "ASP-mainserviceplan-f1e6"
  os_type             = "Windows"
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  sku_name            = "Y1"
}

resource "azurerm_windows_function_app" "main_azure_function_app" {
  app_settings = {
    WEBSITE_RUN_FROM_PACKAGE               = "0"
    FUNCTIONS_WORKER_RUNTIME               = "dotnet-isolated"
    WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED = "1"
    APPINSIGHTS_INSTRUMENTATIONKEY         = azurerm_application_insights.main_application_insights.instrumentation_key
    APPLICATIONINSIGHTS_CONNECTION_STRING  = azurerm_application_insights.main_application_insights.connection_string
    AZURE_TENANT_ID                        = azurerm_user_assigned_identity.openai_identity.tenant_id
    AZURE_CLIENT_ID                        = azurerm_user_assigned_identity.openai_identity.client_id
    COSMOS_CONNECTION                      = azurerm_cosmosdb_account.ticket_db.primary_sql_connection_string
    CosmosDb__Endpoint                     = azurerm_cosmosdb_account.ticket_db.endpoint
    CosmosDb__DatabaseId                   = azurerm_cosmosdb_sql_database.tickets.name
    CosmosDb__ContainerId                  = azurerm_cosmosdb_sql_container.ticket_container.name
    AzureOpenAI__Endpoint                  = local.openai_endpoint
    AzureOpenAI__DeploymentName            = var.model_deployment_name
    AzureOpenAI__KnowledgeBaseMCPServerUrl = "https://${azurerm_windows_web_app.mcp_server.default_hostname}/api/mcp"
  }

  builtin_logging_enabled                  = true
  client_certificate_mode                  = "Required"
  ftp_publish_basic_authentication_enabled = false
  location                                 = var.location
  name                                     = "mainfuncappcosmostriggerb74b"
  resource_group_name                      = data.azurerm_resource_group.main_resource_group.name
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
      azurerm_user_assigned_identity.openai_identity.id,
    ]
  }
}

resource "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                = "log-main-latency-workspace"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "main_application_insights" {
  application_type    = "web"
  location            = var.location
  name                = "appi_mainfuncapp_appinsights"
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  sampling_percentage = 0
  workspace_id        = azurerm_log_analytics_workspace.log_analytics_workspace.id
}

resource "azurerm_monitor_action_group" "smart_detection_rule_action_group" {
  name                = "Application Insights Smart Detection"
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  short_name          = "SmartDetect"
  arm_role_receiver {
    name                    = "Monitoring Contributor"
    role_id                 = "749f88d5-cbae-40b8-bcfc-e573ddc772fa"
    use_common_alert_schema = true
  }
  arm_role_receiver {
    name                    = "Monitoring Reader"
    role_id                 = "43d0d8ad-25c7-4714-9337-8ba259a9fe05"
    use_common_alert_schema = true
  }
}

resource "azurerm_monitor_smart_detector_alert_rule" "azure_function_smart_detection_rule" {
  description         = "Failure Anomalies notifies you of an unusual rise in the rate of failed HTTP requests or dependency calls."
  detector_type       = "FailureAnomaliesDetector"
  frequency           = "PT1M"
  name                = "Failure Anomalies - Main Azure Function"
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  scope_resource_ids = [
    azurerm_application_insights.main_application_insights.id,
  ]
  severity = "Sev3"
  action_group {
    ids = [
      azurerm_monitor_action_group.smart_detection_rule_action_group.id,
    ]
  }
}

resource "azurerm_storage_account" "azure_function_storage_account" {
  account_kind                    = "Storage"
  account_replication_type        = "LRS"
  account_tier                    = "Standard"
  allow_nested_items_to_be_public = false
  default_to_oauth_authentication = true
  location                        = var.location
  name                            = "safunctionapp98a78ba6"
  resource_group_name             = data.azurerm_resource_group.main_resource_group.name
}

resource "azurerm_storage_container" "azure_function_webjobs_hosts_container" {
  name               = "azure-webjobs-hosts"
  storage_account_id = azurerm_storage_account.azure_function_storage_account.id
}

resource "azurerm_storage_container" "azure_function_webjobs_secrets_container" {
  name               = "azure-webjobs-secrets"
  storage_account_id = azurerm_storage_account.azure_function_storage_account.id
}

resource "azurerm_storage_share" "azure_function_file_share" {
  name               = "mainfuncapp9cca"
  quota              = 102400
  storage_account_id = azurerm_storage_account.azure_function_storage_account.id
}

resource "azurerm_storage_account_queue_properties" "azure_function_storage_queue_properties" {
  storage_account_id = azurerm_storage_account.azure_function_storage_account.id
  hour_metrics {
    version = "1.0"
  }
  logging {
    version               = "1.0"
    delete                = false
    read                  = false
    write                 = false
    retention_policy_days = 2
  }
  minute_metrics {
    version = "1.0"
  }
}
