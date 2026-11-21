resource "azurerm_storage_account" "state_storage_account" {
  name                            = "stevhidemstate67fd"
  resource_group_name             = azurerm_resource_group.main_resource_group.name
  location                        = var.location
  account_kind                    = "StorageV2"
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  access_tier                     = "Hot"
  is_hns_enabled                  = false
  allow_nested_items_to_be_public = false
  default_to_oauth_authentication = true
}

resource "azurerm_storage_table" "orders_table" {
  name               = "orders"
  storage_account_id = azurerm_storage_account.state_storage_account.id
}

resource "azurerm_storage_table" "inventory_table" {
  name               = "inventory"
  storage_account_id = azurerm_storage_account.state_storage_account.id
}

resource "azurerm_role_assignment" "stg_role_assignment_table_data_contributor" {
  scope                = azurerm_storage_account.state_storage_account.id
  role_definition_name = "Storage Table Data Contributor"
  principal_id         = var.user_principal_id
}
