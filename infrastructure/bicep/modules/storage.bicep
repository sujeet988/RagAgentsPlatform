param location string
param appName string
param environment string
param tags object
param deployPrivateEndpoint bool = false
param privateEndpointSubnetId string = ''
param storageBlobPrivateDnsZoneId string = ''

var storageAccountName = 'st${replace(appName, '-', '')}${environment}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: environment == 'prod' ? 'Standard_ZRS' : 'Standard_LRS'
  }
  kind: 'StorageV2'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    publicNetworkAccess: deployPrivateEndpoint ? 'Disabled' : 'Enabled'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true // Required for Azure Functions
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: deployPrivateEndpoint ? 'Deny' : 'Allow'
    }
  }
}

// Blob service
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: environment == 'prod' ? 30 : 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: environment == 'prod' ? 30 : 7
    }
  }
}

// Containers
resource pdfsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'pdfs'
  properties: {
    publicAccess: 'None'
  }
}

// Private endpoint for blob
resource blobPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = if (deployPrivateEndpoint) {
  name: 'pe-${storageAccountName}-blob'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'blob-connection'
        properties: {
          privateLinkServiceId: storageAccount.id
          groupIds: ['blob']
        }
      }
    ]
  }
}

resource blobDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = if (deployPrivateEndpoint) {
  parent: blobPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'blob-config'
        properties: {
          privateDnsZoneId: storageBlobPrivateDnsZoneId
        }
      }
    ]
  }
}

output storageAccountId string = storageAccount.id
output storageAccountName string = storageAccount.name
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output principalId string = storageAccount.identity.principalId
