param location string
param appName string
param environment string
param tags object
param vnetId string = ''
param privateEndpointSubnetId string = ''
param deployPrivateEndpoint bool = false

var searchName = 'search-${appName}-${environment}'
var privateEndpointName = 'pe-${searchName}'
var sku = environment == 'prod' ? 'standard' : 'basic'

resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchName
  location: location
  tags: tags
  sku: {
    name: sku
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    replicaCount: environment == 'prod' ? 2 : 1
    partitionCount: environment == 'prod' ? 2 : 1
    hostingMode: 'default'
    publicNetworkAccess: deployPrivateEndpoint ? 'disabled' : 'enabled'
    networkRuleSet: deployPrivateEndpoint ? {
      ipRules: []
    } : null
    disableLocalAuth: false // Set to true when fully using Managed Identity
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http401WithBearerChallenge'
      }
    }
    semanticSearch: 'free'
  }
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
          privateLinkServiceId: search.id
          groupIds: [
            'searchService'
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
          privateDnsZoneId: resourceId('Microsoft.Network/privateDnsZones', 'privatelink.search.windows.net')
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'diagnostics'
  scope: search
  properties: {
    workspaceId: resourceId('Microsoft.OperationalInsights/workspaces', 'log-${appName}-${environment}')
    logs: [
      {
        category: 'OperationLogs'
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

output searchId string = search.id
output searchName string = search.name
output endpoint string = 'https://${search.name}.search.windows.net'
output principalId string = search.identity.principalId
