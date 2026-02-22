param location string
param appName string
param environment string
param tags object
param vnetId string = ''
param privateEndpointSubnetId string = ''
param deployPrivateEndpoint bool = false

var openAIName = 'openai-${appName}-${environment}'
var privateEndpointName = 'pe-${openAIName}'

resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIName
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: deployPrivateEndpoint ? 'Disabled' : 'Enabled'
    networkAcls: deployPrivateEndpoint ? {
      defaultAction: 'Deny'
    } : {
      defaultAction: 'Allow'
    }
  }
}

// GPT-4 Deployment
resource gpt4Deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'gpt-4'
  sku: {
    name: 'Standard'
    capacity: environment == 'prod' ? 40 : 20
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4'
      version: '0613'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.Default'
  }
}

// Embedding Deployment
resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'text-embedding-3-large'
  sku: {
    name: 'Standard'
    capacity: environment == 'prod' ? 80 : 40
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-3-large'
      version: '1'
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
    raiPolicyName: 'Microsoft.Default'
  }
  dependsOn: [
    gpt4Deployment
  ]
}

// Private Endpoint
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-04-01' = if (deployPrivateEndpoint && !empty(vnetId)) {
  name: privateEndpointName
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: privateEndpointName
        properties: {
          privateLinkServiceId: openAI.id
          groupIds: [
            'account'
          ]
        }
      }
    ]
  }
}

// Private DNS Zone Group
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-04-01' = if (deployPrivateEndpoint && !empty(vnetId)) {
  parent: privateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'config'
        properties: {
          privateDnsZoneId: resourceId('Microsoft.Network/privateDnsZones', 'privatelink.openai.azure.com')
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diagnostics'
  scope: openAI
  properties: {
    workspaceId: resourceId('Microsoft.OperationalInsights/workspaces', 'log-${appName}-${environment}')
    logs: [
      {
        category: 'Audit'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 90 : 30
        }
      }
      {
        category: 'RequestResponse'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 90 : 30
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 90 : 30
        }
      }
    ]
  }
}

output openAIId string = openAI.id
output openAIName string = openAI.name
output endpoint string = openAI.properties.endpoint
output principalId string = openAI.identity.principalId
