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

variable "partition_count" {
  type    = number
  default = 1
}

