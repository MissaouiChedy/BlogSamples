locals {
  resources_suffix = "f1cc"
  foundry_endpoint = "https://${var.ai_foundry_name}.services.ai.azure.com/api/projects/${var.ai_foundry_project_name}"
  openai_endpoint  = "https://${var.ai_foundry_name}.cognitiveservices.azure.com"
}