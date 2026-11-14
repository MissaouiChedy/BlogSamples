resource "azurerm_managed_redis" "idempotency_redis" {
  name                      = "amr-evh-idem-67fd"
  resource_group_name       = azurerm_resource_group.main_resource_group.name
  location                  = var.location
  sku_name                  = "Balanced_B0"
  high_availability_enabled = false

  default_database {
    access_keys_authentication_enabled = false
    clustering_policy                  = "NoCluster"
    eviction_policy                    = "VolatileTTL"
  }
}

resource "azapi_resource" "redis_user_data_access" {
  type      = "Microsoft.Cache/redisEnterprise/databases/accessPolicyAssignments@2025-04-01"
  name      = "RedisUserDataAccess"
  parent_id = azurerm_managed_redis.idempotency_redis.default_database[0].id
  body = {
    properties = {
      accessPolicyName = "default"
      user = {
        objectId = var.user_principal_id
      }
    }
  }
}
