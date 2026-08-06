resource "azurerm_user_assigned_identity" "main_load_test_identity" {
  name                = "id-main-load-test-identity"
  resource_group_name = azurerm_resource_group.main_resource_group.name
  location            = azurerm_resource_group.main_resource_group.location
}