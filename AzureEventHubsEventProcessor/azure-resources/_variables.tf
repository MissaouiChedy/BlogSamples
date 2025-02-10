variable "subscription" {
  type        = string
  description = "Azure subscription UUID"
}

variable "userPrincipalId" {
  type        = string
  description = "User principal UUID having access on the resources"
}

variable "location" {
  type    = string
  default = "eastus"
}

variable "partitionCount" {
  type    = number
  default = 3
}

