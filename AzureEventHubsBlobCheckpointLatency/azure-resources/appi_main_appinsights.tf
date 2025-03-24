resource "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                = "log-main-latency-workspace"
  location            = var.location
  resource_group_name = azurerm_resource_group.main_resource_group.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_application_insights" "main_application_insights" {
  application_type    = "web"
  location            = var.location
  name                = "appi_mainfuncapp_appinsights"
  resource_group_name = azurerm_resource_group.main_resource_group.name
  sampling_percentage = 0
  workspace_id        = azurerm_log_analytics_workspace.log_analytics_workspace.id
}

resource "azurerm_monitor_action_group" "smart_detection_rule_action_group" {
  name                = "Application Insights Smart Detection"
  resource_group_name = azurerm_resource_group.main_resource_group.name
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
  resource_group_name = azurerm_resource_group.main_resource_group.name
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
