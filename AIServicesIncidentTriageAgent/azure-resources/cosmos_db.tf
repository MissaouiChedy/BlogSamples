
resource "azurerm_cosmosdb_account" "ticket_db" {
  name                = "cosmos-ticket-classification-${local.resources_suffix}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  automatic_failover_enabled = false

  capabilities {
    name = "EnableServerless"
  }

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }
}

resource "azurerm_cosmosdb_sql_database" "tickets" {
  name                = "TicketDB"
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
}

resource "azurerm_cosmosdb_sql_container" "ticket_container" {
  name                = "Tickets"
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
  database_name       = azurerm_cosmosdb_sql_database.tickets.name
  partition_key_paths = ["/Category"]
}

resource "azurerm_cosmosdb_sql_container" "resolution_container" {
  name                = "Resolutions"
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
  database_name       = azurerm_cosmosdb_sql_database.tickets.name
  partition_key_paths = ["/Category"]
}

resource "azapi_resource" "knowledge_base_container" {
  type      = "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2025-05-01-preview"
  name      = "KnowledgeBase"
  parent_id = azurerm_cosmosdb_sql_database.tickets.id
  location  = data.azurerm_resource_group.main_resource_group.location
  body = {
    properties = {
      resource = {
        id = "KnowledgeBase"
        partitionKey = {
          paths = ["/Category"]
        }

        fullTextPolicy = {
          defaultLanguage = "en-US"
          fullTextPaths = [
            {
              path     = "/Title"
              language = "en-US"
            },
            {
              path     = "/Issue"
              language = "en-US"
            },
            {
              path     = "/Solution"
              language = "en-US"
            },
            {
              path     = "/Discussion"
              language = "en-US"
            }
          ]
        }
      }
    }
  }
}

resource "azurerm_cosmosdb_sql_container" "ticket_lease_container" {
  name                = "leases"
  resource_group_name = data.azurerm_resource_group.main_resource_group.name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
  database_name       = azurerm_cosmosdb_sql_database.tickets.name
  partition_key_paths = ["/id"]
}

resource "azurerm_role_assignment" "cosmos_identity_role" {
  scope                = azurerm_cosmosdb_account.ticket_db.id
  role_definition_name = "Contributor"
  principal_id         = azurerm_user_assigned_identity.openai_identity.principal_id
}

data "azurerm_cosmosdb_sql_role_definition" "cosmos_db_built_in_data_contributor" {
  resource_group_name = azurerm_cosmosdb_account.ticket_db.resource_group_name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
  role_definition_id  = "00000000-0000-0000-0000-000000000002"
}

data "azurerm_cosmosdb_sql_role_definition" "cosmos_db_built_in_data_reader" {
  resource_group_name = azurerm_cosmosdb_account.ticket_db.resource_group_name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
  role_definition_id  = "00000000-0000-0000-0000-000000000001"
}

resource "azurerm_cosmosdb_sql_role_assignment" "developer_access" {
  name                = "39ae1b57-421c-4fb7-9ad6-f8d466d90ab1"
  resource_group_name = azurerm_cosmosdb_account.ticket_db.resource_group_name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
  role_definition_id  = data.azurerm_cosmosdb_sql_role_definition.cosmos_db_built_in_data_contributor.id
  principal_id        = var.user_principal_id
  scope               = azurerm_cosmosdb_account.ticket_db.id
}

resource "azurerm_cosmosdb_sql_role_assignment" "developer_access_2" {
  name                = "6a677ef8-e10e-4808-a260-a0374a8c0664"
  resource_group_name = azurerm_cosmosdb_account.ticket_db.resource_group_name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
  role_definition_id  = data.azurerm_cosmosdb_sql_role_definition.cosmos_db_built_in_data_reader.id
  principal_id        = var.user_principal_id
  scope               = azurerm_cosmosdb_account.ticket_db.id
}

resource "azurerm_cosmosdb_sql_role_assignment" "managed_identity_access" {
  name                = "17ce1616-9371-4ae0-b410-d9967c23546d"
  resource_group_name = azurerm_cosmosdb_account.ticket_db.resource_group_name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
  role_definition_id  = data.azurerm_cosmosdb_sql_role_definition.cosmos_db_built_in_data_contributor.id
  principal_id        = azurerm_user_assigned_identity.openai_identity.principal_id
  scope               = azurerm_cosmosdb_account.ticket_db.id
}

resource "azurerm_cosmosdb_sql_role_assignment" "managed_identity_access_2" {
  name                = "a735b022-67be-4e17-a560-1aa2f80462c5"
  resource_group_name = azurerm_cosmosdb_account.ticket_db.resource_group_name
  account_name        = azurerm_cosmosdb_account.ticket_db.name
  role_definition_id  = data.azurerm_cosmosdb_sql_role_definition.cosmos_db_built_in_data_reader.id
  principal_id        = azurerm_user_assigned_identity.openai_identity.principal_id
  scope               = azurerm_cosmosdb_account.ticket_db.id
}
