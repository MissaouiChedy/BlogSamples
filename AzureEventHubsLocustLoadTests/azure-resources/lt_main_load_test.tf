resource "azurerm_load_test" "main_load_test" {
  location            = azurerm_resource_group.main_resource_group.location
  name                = "lt-main-loadtest-${local.suffix}"
  resource_group_name = azurerm_resource_group.main_resource_group.name

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.main_load_test_identity.id]
  }
}
