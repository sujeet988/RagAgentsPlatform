param location string
param appName string
param environment string
param appServicePlanId string
param applicationInsightsConnectionString string
param keyVaultName string
param tenantId string
param tags object
param vnetId string = ''
param appServiceSubnetId string = ''
param deployVNetIntegration bool = false

var appServiceName = 'app-${appName}-api-${environment}'

resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: appServiceName
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    clientAffinityEnabled: false
    virtualNetworkSubnetId: deployVNetIntegration ? appServiceSubnetId : null
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: environment == 'prod'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      http20Enabled: true
      healthCheckPath: '/health'
      cors: {
        allowedOrigins: environment == 'prod' ? [] : ['*']
        supportCredentials: false
      }
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
        }
        {
          name: 'KeyVaultName'
          value: keyVaultName
        }
        {
          name: 'AzureAd__TenantId'
          value: tenantId
        }
        {
          name: 'AzureAd__Instance'
          value: 'https://login.microsoftonline.com/'
        }
        // OpenTelemetry settings
        {
          name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
          value: 'https://otlp.applicationinsights.azure.com/'
        }
        {
          name: 'OTEL_SERVICE_NAME'
          value: appServiceName
        }
        {
          name: 'OTEL_RESOURCE_ATTRIBUTES'
          value: 'service.name=${appServiceName},service.namespace=${appName},deployment.environment=${environment}'
        }
      ]
    }
  }
}

// Staging Slot for Production
resource stagingSlot 'Microsoft.Web/sites/slots@2023-01-01' = if (environment == 'prod') {
  parent: appService
  name: 'staging'
  location: location
  tags: tags
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      appSettings: appService.properties.siteConfig.appSettings
    }
  }
}

output appServiceId string = appService.id
output appServiceName string = appService.name
output appUrl string = 'https://${appService.properties.defaultHostName}'
output principalId string = appService.identity.principalId
output stagingPrincipalId string = environment == 'prod' ? stagingSlot.identity.principalId : ''
