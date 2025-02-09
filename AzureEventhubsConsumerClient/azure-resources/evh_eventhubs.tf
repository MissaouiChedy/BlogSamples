resource "azurerm_eventhub_namespace" "main_eventhub_namespace" {
  name                          = "evh-test-consumer-client"
  resource_group_name           = azurerm_resource_group.main_resource_group.name
  location                      = var.location
  sku                           = "Standard"
  capacity                      = 1
  local_authentication_enabled  = true
  auto_inflate_enabled          = false
  public_network_access_enabled = true
  identity {
    type = "SystemAssigned"
  }

}

resource "azurerm_eventhub" "main_eventhub_topic" {
  name              = "main-topic"
  namespace_id      = azurerm_eventhub_namespace.main_eventhub_namespace.id
  partition_count   = var.partitionCount
  message_retention = 1
  status            = "Active"
}

resource "azurerm_eventhub_consumer_group" "main_eventhub_consumer_group" {
  name                = "main-consumer"
  namespace_name      = azurerm_eventhub_namespace.main_eventhub_namespace.name
  eventhub_name       = azurerm_eventhub.main_eventhub_topic.name
  resource_group_name = azurerm_resource_group.main_resource_group.name
}

resource "azurerm_role_assignment" "evh_role_assignment_data_sender" {
  scope                = azurerm_eventhub.main_eventhub_topic.id
  role_definition_name = "Azure Event Hubs Data Sender"
  principal_id         = var.userPrincipalId
}

resource "azurerm_role_assignment" "evh_role_assignment_data_receiver" {
  scope                = azurerm_eventhub.main_eventhub_topic.id
  role_definition_name = "Azure Event Hubs Data Receiver"
  principal_id         = var.userPrincipalId
}
