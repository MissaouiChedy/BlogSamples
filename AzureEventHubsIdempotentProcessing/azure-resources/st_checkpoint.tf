resource "azurerm_storage_account" "checkpoint_storage_account" {
  name                            = "stevhidemcheckpoint67fd"
  resource_group_name             = azurerm_resource_group.main_resource_group.name
  location                        = var.location
  account_kind                    = "StorageV2"
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  access_tier                     = "Hot"
  is_hns_enabled                  = false
  allow_nested_items_to_be_public = false
  default_to_oauth_authentication = true

  blob_properties {
    versioning_enabled = false

    delete_retention_policy {
      permanent_delete_enabled = false
    }
  }
}

resource "azurerm_storage_container" "checkpoint_container" {
  name               = "checkpoint"
  storage_account_id = azurerm_storage_account.checkpoint_storage_account.id
}

resource "azurerm_role_assignment" "stg_role_assignment_blob_data_owner" {
  scope                = azurerm_storage_account.checkpoint_storage_account.id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = var.user_principal_id
}
