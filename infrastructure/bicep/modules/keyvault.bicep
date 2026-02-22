param location string
param appName string
param environment string
param tenantId string
param tags object
param deployPrivateEndpoint bool = false
param privateEndpointSubnetId string = ''
param keyVaultPrivateDnsZoneId string = ''

var keyVaultName = 'kv-${appName}-${environment}'

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: environment == 'prod' ? 90 : 7
    enablePurgeProtection: environment == 'prod'
    publicNetworkAccess: deployPrivateEndpoint ? 'Disabled' : 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: deployPrivateEndpoint ? 'Deny' : 'Allow'
    }
  }
}

// Private endpoint
resource keyVaultPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-05-01' = if (deployPrivateEndpoint) {
  name: 'pe-${keyVaultName}'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'keyvault-connection'
        properties: {
          privateLinkServiceId: keyVault.id
          groupIds: ['vault']
        }
      }
    ]
  }
}

resource keyVaultDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-05-01' = if (deployPrivateEndpoint) {
  parent: keyVaultPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'keyvault-config'
        properties: {
          privateDnsZoneId: keyVaultPrivateDnsZoneId
        }
      }
    ]
  }
}

output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
