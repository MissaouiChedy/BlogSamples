resource "azurerm_redis_cache" "main_redis_cache" {
  name                          = "rds-latency-cache-store"
  location                      = var.location
  resource_group_name           = azurerm_resource_group.main_resource_group.name
  capacity                      = 0
  family                        = "C"
  sku_name                      = "Basic"
  non_ssl_port_enabled          = false
  redis_version                 = "6"
  public_network_access_enabled = true

  minimum_tls_version = "1.2"

  redis_configuration {
    aof_backup_enabled                      = false
    active_directory_authentication_enabled = true
  }
}

resource "azurerm_redis_cache_access_policy_assignment" "redis_data_owner" {
  name               = "redis_data_owner"
  redis_cache_id     = azurerm_redis_cache.main_redis_cache.id
  access_policy_name = "Data Owner"
  object_id          = var.userPrincipalId
  object_id_alias    = "Main Data Owner"
}

resource "azurerm_redis_cache_access_policy_assignment" "func_data_contributor" {
  name               = "func_data_contributor"
  redis_cache_id     = azurerm_redis_cache.main_redis_cache.id
  access_policy_name = "Data Contributor"
  object_id          = azurerm_user_assigned_identity.main_func_identity.principal_id
  object_id_alias    = "Func Data Contributor"
}
