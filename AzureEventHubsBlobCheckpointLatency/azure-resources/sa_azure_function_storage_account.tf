resource "azurerm_storage_account" "azure_function_storage_account" {
  account_kind                    = "Storage"
  account_replication_type        = "LRS"
  account_tier                    = "Standard"
  allow_nested_items_to_be_public = false
  default_to_oauth_authentication = true
  location                        = var.location
  name                            = "safunctionapp98a78ba6"
  resource_group_name             = azurerm_resource_group.main_resource_group.name
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
