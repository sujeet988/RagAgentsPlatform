param location string
param appName string
param environment string
param tags object
param deployPrivateEndpoint bool = false
param privateEndpointSubnetId string = ''
param cosmosDbPrivateDnsZoneId string = ''

var accountName = 'cosmos-${appName}-${environment}'
var databaseName = 'rag-chat-db'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: accountName
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: environment == 'prod'
      }
    ]
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: environment == 'prod' ? 240 : 1440
        backupRetentionIntervalInHours: environment == 'prod' ? 720 : 168
      }
    }
    publicNetworkAccess: deployPrivateEndpoint ? 'Disabled' : 'Enabled'
    networkAclBypass: 'AzureServices'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmosAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
    options: {
      throughput: environment == 'prod' ? 1000 : 400
    }
  }
}

// Conversations container
resource conversationsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: database
  name: 'conversations'
  properties: {
    resource: {
      id: 'conversations'
      partitionKey: {
        paths: ['/userId']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
      defaultTtl: -1
    }
  }
}

// Ingestion jobs container
resource jobsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: database
  name: 'ingestion-jobs'
  properties: {
    resource: {
      id: 'ingestion-jobs'
      partitionKey: {
        paths: ['/id']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
      defaultTtl: 2592000 // 30 days
    }
  }
}

// Search analytics container
resource analyticsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  parent: database
  name: 'search-analytics'
  properties: {
    resource: {
      id: 'search-analytics'
      partitionKey: {
        paths: ['/userId']
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
      }
      defaultTtl: 7776000 // 90 days
    }
  }
}

// Private endpoint
resource cosmosPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = if (deployPrivateEndpoint) {
  name: 'pe-${accountName}'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'cosmos-connection'
        properties: {
          privateLinkServiceId: cosmosAccount.id
          groupIds: ['Sql']
        }
      }
    ]
  }
}

resource cosmosDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = if (deployPrivateEndpoint) {
  parent: cosmosPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'cosmos-config'
        properties: {
          privateDnsZoneId: cosmosDbPrivateDnsZoneId
        }
      }
    ]
  }
}

output cosmosDbId string = cosmosAccount.id
output cosmosDbName string = cosmosAccount.name
output cosmosDbEndpoint string = cosmosAccount.properties.documentEndpoint
output databaseName string = database.name
output principalId string = cosmosAccount.identity.principalId
