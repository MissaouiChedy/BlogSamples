resource "azurerm_storage_account" "main_storage_account" {
  account_kind                    = "BlobStorage"
  account_replication_type        = "LRS"
  account_tier                    = "Standard"
  access_tier                     = "Hot"
  is_hns_enabled                  = false
  allow_nested_items_to_be_public = false
  default_to_oauth_authentication = true
  location                        = var.location
  name                            = "samainacclatencytestfc4a"
  resource_group_name             = azurerm_resource_group.main_resource_group.name

  blob_properties {
    versioning_enabled = false

    delete_retention_policy {
      permanent_delete_enabled = false
    }
  }
}

resource "azurerm_storage_container" "checkpoint_container" {
  name               = "checkpoint"
  storage_account_id = azurerm_storage_account.main_storage_account.id
}

resource "azurerm_storage_blob" "example_blob" {
  name                   = "0"
  storage_account_name   = azurerm_storage_account.main_storage_account.name
  storage_container_name = azurerm_storage_container.checkpoint_container.name
  type                   = "Block"
  source_content         = "kablam"
}

resource "azurerm_role_assignment" "storage_account_owner" {
  scope                = azurerm_storage_account.main_storage_account.id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = var.userPrincipalId
}

resource "azurerm_role_assignment" "storage_account_contributor" {
  scope                = azurerm_storage_container.checkpoint_container.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.main_func_identity.principal_id
}
