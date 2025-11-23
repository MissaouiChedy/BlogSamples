resource "azurerm_managed_redis" "main_redis_cache" {
  name                      = "rds-test-consumer-client"
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

resource "azapi_resource" "redis_data_user_access" {
  type      = "Microsoft.Cache/redisEnterprise/databases/accessPolicyAssignments@2025-04-01"
  name      = "RedisDataUserAccess"
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
