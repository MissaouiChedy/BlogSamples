resource "azurerm_user_assigned_identity" "openai_identity" {
  name                = "id-openai-ticket-classification"
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  location            = var.location
}

resource "azurerm_role_assignment" "openai_identity_role_foundry" {
  scope                = data.azurerm_cognitive_account.main_ai_foundry.id
  role_definition_name = "Azure AI User"
  principal_id         = azurerm_user_assigned_identity.openai_identity.principal_id
}
