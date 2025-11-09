data "azurerm_cognitive_account" "main_ai_foundry" {
  name                = var.ai_foundry_name
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
}
