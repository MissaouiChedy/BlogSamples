resource "azurerm_user_assigned_identity" "main_func_identity" {
  name                = "id-main-func-identity"
  resource_group_name = azurerm_resource_group.main_resource_group.name
  location            = var.location
}