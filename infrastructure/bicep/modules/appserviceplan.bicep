param location string
param appName string
param environment string
param tags object

var planName = 'asp-${appName}-${environment}'
var sku = environment == 'prod' ? 'P1v3' : 'B1'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: sku
    tier: environment == 'prod' ? 'PremiumV3' : 'Basic'
    capacity: environment == 'prod' ? 2 : 1
  }
  kind: 'linux'
  properties: {
    reserved: true
    zoneRedundant: environment == 'prod'
  }
}

output appServicePlanId string = appServicePlan.id
output appServicePlanName string = appServicePlan.name
