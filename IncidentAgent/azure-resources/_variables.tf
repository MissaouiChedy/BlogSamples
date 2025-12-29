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
  default = "aif-main-foundry-f1cc"
}

variable "ai_foundry_project_name" {
  type    = string
  default = "proj-main-f1cc"
}

variable "model_deployment_name" {
  type    = string
  default = "incident-classification-gpt41"
}
