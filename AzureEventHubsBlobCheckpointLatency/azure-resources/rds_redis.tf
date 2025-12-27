# resource "azurerm_redis_cache" "main_redis_cache_old" {
#   name                          = "rds-latency-cache-store-old"
#   location                      = var.location
#   resource_group_name           = azurerm_resource_group.main_resource_group.name
#   capacity                      = 0
#   family                        = "C"
#   sku_name                      = "Basic"
#   non_ssl_port_enabled          = false
#   redis_version                 = "6"
#   public_network_access_enabled = true

#   minimum_tls_version = "1.2"

#   redis_configuration {
#     aof_backup_enabled                      = false
#     active_directory_authentication_enabled = true
#   }
# }

# resource "azurerm_redis_cache_access_policy_assignment" "redis_data_owner" {
#   name               = "redis_data_owner"
#   redis_cache_id     = azurerm_redis_cache.main_redis_cache_old.id
#   access_policy_name = "Data Owner"
#   object_id          = var.userPrincipalId
#   object_id_alias    = "Main Data Owner"
# }

# resource "azurerm_redis_cache_access_policy_assignment" "func_data_contributor" {
#   name               = "func_data_contributor"
#   redis_cache_id     = azurerm_redis_cache.main_redis_cache_old.id
#   access_policy_name = "Data Contributor"
#   object_id          = azurerm_user_assigned_identity.main_func_identity.principal_id
#   object_id_alias    = "Func Data Contributor"
# }

resource "azurerm_managed_redis" "main_redis_cache" {
  name                      = "rds-latency-cache-store"
  resource_group_name       = azurerm_resource_group.main_resource_group.name
  location                  = azurerm_resource_group.main_resource_group.location
  sku_name                  = "Balanced_B0"
  high_availability_enabled = false

  default_database {
    access_keys_authentication_enabled = false
    clustering_policy                  = "NoCluster"
    eviction_policy                    = "VolatileTTL"
  }
}

resource "azapi_resource" "redis_main_user_data_access" {
  type      = "Microsoft.Cache/redisEnterprise/databases/accessPolicyAssignments@2025-04-01"
  name      = "RedisMainUserDataAccess"
  parent_id = azurerm_managed_redis.main_redis_cache.default_database[0].id
  body = {
    properties = {
      accessPolicyName = "default"
      user = {
        objectId = var.userPrincipalId
      }
    }
  }
}

resource "azapi_resource" "redis_managed_id_data_access" {
  type      = "Microsoft.Cache/redisEnterprise/databases/accessPolicyAssignments@2025-04-01"
  name      = "RedisManagedIdDataAccess"
  parent_id = azurerm_managed_redis.main_redis_cache.default_database[0].id
  body = {
    properties = {
      accessPolicyName = "default"
      user = {
        objectId = azurerm_user_assigned_identity.main_func_identity.principal_id
      }
    }
  }
}

