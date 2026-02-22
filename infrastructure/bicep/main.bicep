targetScope = 'subscription'

@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Azure region for resources')
param location string = 'eastus'

@description('Application name')
param appName string = 'ragagents'

@description('Azure AD Tenant ID')
param tenantId string

@description('Deploy private endpoints')
param deployPrivateEndpoints bool = false

@description('Tags for all resources')
param tags object = {
  Environment: environment
  Application: appName
  ManagedBy: 'Bicep'
}

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-${appName}-${environment}'
  location: location
  tags: tags
}

// Networking (VNet and Private DNS Zones)
module networking 'modules/networking.bicep' = if (deployPrivateEndpoints) {
  scope: rg
  name: 'networking-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    tags: tags
  }
}

// Key Vault
module keyVault 'modules/keyvault.bicep' = {
  scope: rg
  name: 'keyvault-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    tenantId: tenantId
    tags: tags
    vnetId: deployPrivateEndpoints ? networking.outputs.vnetId : ''
    privateEndpointSubnetId: deployPrivateEndpoints ? networking.outputs.privateEndpointSubnetId : ''
    deployPrivateEndpoint: deployPrivateEndpoints
  }
}

// Application Insights
module appInsights 'modules/monitoring.bicep' = {
  scope: rg
  name: 'monitoring-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    tags: tags
  }
}

// Storage Account
module storage 'modules/storage.bicep' = {
  scope: rg
  name: 'storage-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    tags: tags
    vnetId: deployPrivateEndpoints ? networking.outputs.vnetId : ''
    privateEndpointSubnetId: deployPrivateEndpoints ? networking.outputs.privateEndpointSubnetId : ''
    deployPrivateEndpoint: deployPrivateEndpoints
  }
}

// Cosmos DB
module cosmosDb 'modules/cosmosdb.bicep' = {
  scope: rg
  name: 'cosmosdb-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    tags: tags
    vnetId: deployPrivateEndpoints ? networking.outputs.vnetId : ''
    privateEndpointSubnetId: deployPrivateEndpoints ? networking.outputs.privateEndpointSubnetId : ''
    deployPrivateEndpoint: deployPrivateEndpoints
  }
}

// Azure OpenAI
module openAI 'modules/openai.bicep' = {
  scope: rg
  name: 'openai-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    tags: tags
    vnetId: deployPrivateEndpoints ? networking.outputs.vnetId : ''
    privateEndpointSubnetId: deployPrivateEndpoints ? networking.outputs.privateEndpointSubnetId : ''
    deployPrivateEndpoint: deployPrivateEndpoints
  }
}

// Azure AI Search
module search 'modules/search.bicep' = {
  scope: rg
  name: 'search-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    tags: tags
    vnetId: deployPrivateEndpoints ? networking.outputs.vnetId : ''
    privateEndpointSubnetId: deployPrivateEndpoints ? networking.outputs.privateEndpointSubnetId : ''
    deployPrivateEndpoint: deployPrivateEndpoints
  }
}

// Document Intelligence
module documentAI 'modules/documentai.bicep' = {
  scope: rg
  name: 'documentai-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    tags: tags
  }
}

// App Service Plan
module appServicePlan 'modules/appserviceplan.bicep' = {
  scope: rg
  name: 'appserviceplan-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    tags: tags
  }
}

// App Service (API)
module appService 'modules/appservice.bicep' = {
  scope: rg
  name: 'appservice-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    appServicePlanId: appServicePlan.outputs.appServicePlanId
    applicationInsightsConnectionString: appInsights.outputs.connectionString
    keyVaultName: keyVault.outputs.keyVaultName
    tenantId: tenantId
    tags: tags
    vnetId: deployPrivateEndpoints ? networking.outputs.vnetId : ''
    appServiceSubnetId: deployPrivateEndpoints ? networking.outputs.appServiceSubnetId : ''
    deployVNetIntegration: deployPrivateEndpoints
  }
  dependsOn: [
    openAI
    search
    cosmosDb
  ]
}

// Azure Functions
module functions 'modules/functions.bicep' = {
  scope: rg
  name: 'functions-deployment'
  params: {
    location: location
    appName: appName
    environment: environment
    appServicePlanId: appServicePlan.outputs.appServicePlanId
    storageAccountName: storage.outputs.storageAccountName
    applicationInsightsConnectionString: appInsights.outputs.connectionString
    keyVaultName: keyVault.outputs.keyVaultName
    tags: tags
    vnetId: deployPrivateEndpoints ? networking.outputs.vnetId : ''
    appServiceSubnetId: deployPrivateEndpoints ? networking.outputs.appServiceSubnetId : ''
    deployVNetIntegration: deployPrivateEndpoints
  }
  dependsOn: [
    storage
    openAI
    search
    documentAI
    cosmosDb
  ]
}

// RBAC Role Assignments
module rbac 'modules/rbac.bicep' = {
  scope: rg
  name: 'rbac-deployment'
  params: {
    appServicePrincipalId: appService.outputs.principalId
    functionsPrincipalId: functions.outputs.principalId
    openAIId: openAI.outputs.openAIId
    searchId: search.outputs.searchId
    cosmosDbId: cosmosDb.outputs.cosmosDbId
    storageAccountId: storage.outputs.storageAccountId
    documentAIId: documentAI.outputs.documentAIId
  }
}

// Outputs
output resourceGroupName string = rg.name
output apiUrl string = appService.outputs.appUrl
output functionUrl string = functions.outputs.functionUrl
output keyVaultName string = keyVault.outputs.keyVaultName
output applicationInsightsName string = appInsights.outputs.appInsightsName
output openAIEndpoint string = openAI.outputs.endpoint
output searchEndpoint string = search.outputs.endpoint
output cosmosDbEndpoint string = cosmosDb.outputs.endpoint
