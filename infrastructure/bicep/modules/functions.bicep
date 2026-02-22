param location string
param appName string
param environment string
param appServicePlanId string
param storageAccountName string
param applicationInsightsConnectionString string
param keyVaultName string
param tags object
param vnetId string = ''
param appServiceSubnetId string = ''
param deployVNetIntegration bool = false

var functionAppName = 'func-${appName}-${environment}'

resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    virtualNetworkSubnetId: deployVNetIntegration ? appServiceSubnetId : null
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      alwaysOn: environment == 'prod'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'KeyVaultName'
          value: keyVaultName
        }
        {
          name: 'WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED'
          value: '1'
        }
        // OpenTelemetry settings
        {
          name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
          value: 'https://otlp.applicationinsights.azure.com/'
        }
        {
          name: 'OTEL_SERVICE_NAME'
          value: functionAppName
        }
        {
          name: 'OTEL_RESOURCE_ATTRIBUTES'
          value: 'service.name=${functionAppName},service.namespace=${appName},deployment.environment=${environment}'
        }
      ]
    }
  }
}

output functionAppId string = functionApp.id
output functionAppName string = functionApp.name
output functionUrl string = 'https://${functionApp.properties.defaultHostName}'
output principalId string = functionApp.identity.principalId
