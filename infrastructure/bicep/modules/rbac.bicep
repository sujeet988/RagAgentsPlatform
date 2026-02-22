param appServicePrincipalId string
param functionsPrincipalId string
param openAIId string
param searchId string
param cosmosDbId string
param storageAccountId string
param documentAIId string

// Azure OpenAI - Cognitive Services OpenAI User role
resource openAIRoleAppService 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAIId, appServicePrincipalId, 'OpenAIUser')
  scope: resourceId('Microsoft.CognitiveServices/accounts', last(split(openAIId, '/')))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource openAIRoleFunction 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAIId, functionsPrincipalId, 'OpenAIUser')
  scope: resourceId('Microsoft.CognitiveServices/accounts', last(split(openAIId, '/')))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: functionsPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Azure AI Search - Search Index Data Contributor
resource searchRoleAppService 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchId, appServicePrincipalId, 'SearchIndexDataContributor')
  scope: resourceId('Microsoft.Search/searchServices', last(split(searchId, '/')))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
    principalId: appServicePrincipalId
    principalType: 'ServicePrincipal'
  }
}

resource searchRoleFunction 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchId, functionsPrincipalId, 'SearchIndexDataContributor')
  scope: resourceId('Microsoft.Search/searchServices', last(split(searchId, '/')))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7')
    principalId: functionsPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Cosmos DB - Cosmos DB Built-in Data Contributor
resource cosmosRoleAppService 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmosDbId, appServicePrincipalId, 'CosmosDBDataContributor')
  parent: resourceId('Microsoft.DocumentDB/databaseAccounts', last(split(cosmosDbId, '/')))
  properties: {
    roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions', last(split(cosmosDbId, '/')), '00000000-0000-0000-0000-000000000002')
    principalId: appServicePrincipalId
    scope: cosmosDbId
  }
}

resource cosmosRoleFunction 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(cosmosDbId, functionsPrincipalId, 'CosmosDBDataContributor')
  parent: resourceId('Microsoft.DocumentDB/databaseAccounts', last(split(cosmosDbId, '/')))
  properties: {
    roleDefinitionId: resourceId('Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions', last(split(cosmosDbId, '/')), '00000000-0000-0000-0000-000000000002')
    principalId: functionsPrincipalId
    scope: cosmosDbId
  }
}

// Storage - Storage Blob Data Contributor
resource storageRoleFunction 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountId, functionsPrincipalId, 'StorageBlobDataContributor')
  scope: resourceId('Microsoft.Storage/storageAccounts', last(split(storageAccountId, '/')))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: functionsPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Document AI - Cognitive Services User
resource documentAIRoleFunction 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(documentAIId, functionsPrincipalId, 'CognitiveServicesUser')
  scope: resourceId('Microsoft.CognitiveServices/accounts', last(split(documentAIId, '/')))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
    principalId: functionsPrincipalId
    principalType: 'ServicePrincipal'
  }
}
