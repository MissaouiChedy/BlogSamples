locals {
  resources_suffix = "a8a2"
  foundry_endpoint = "https://${var.ai_foundry_name}.services.ai.azure.com/api/projects/${var.ai_foundry_project_name}"
  openai_endpoint  = "https://${var.ai_foundry_name}.cognitiveservices.azure.com"
}