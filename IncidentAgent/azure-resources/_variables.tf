variable "subscription" {
  type        = string
  description = "Azure subscription UUID"
}

variable "user_principal_id" {
  type        = string
  description = "User principal UUID having access on the resources"
}

variable "location" {
  type    = string
  default = "swedencentral"
}

variable "resource_group_name" {
  type    = string
  default = "rg-test-ticket-classification"
}

variable "ai_foundry_name" {
  type    = string
  default = "aif-main-foundry-a8a2"
}

variable "ai_foundry_project_name" {
  type    = string
  default = "proj-main-a8a2"
}

variable "model_deployment_name" {
  type    = string
  default = "gpt-5.4-mini"
}

variable "cosmos_db_account_name" {
  type    = string
  default = "cosmos-ticket-classification-a8a2"
}
