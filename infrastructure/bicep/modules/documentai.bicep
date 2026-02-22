param location string
param appName string
param environment string
param tags object

var documentAIName = 'docai-${appName}-${environment}'

resource documentAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: documentAIName
  location: location
  tags: tags
  sku: {
    name: environment == 'prod' ? 'S0' : 'F0'
  }
  kind: 'FormRecognizer'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: documentAIName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

output documentAIId string = documentAI.id
output documentAIName string = documentAI.name
output documentAIEndpoint string = documentAI.properties.endpoint
output principalId string = documentAI.identity.principalId
