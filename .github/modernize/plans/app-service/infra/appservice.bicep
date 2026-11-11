targetScope = 'resourceGroup'

param appName string
param location string = resourceGroup().location

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'asp-${appName}'
  location: location
  kind: 'app'
  sku: {
    name: 'B1'
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    reserved: false
  }
}

resource app 'Microsoft.Web/sites@2024-04-01' = {
  name: appName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      use32BitWorkerProcess: false
      alwaysOn: true
      webSocketsEnabled: true
      http20Enabled: true
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
    }
  }
}

resource settings 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: app
  name: 'appsettings'
  properties: {
    ASPNETCORE_ENVIRONMENT: 'Production'
    DOTNET_ENVIRONMENT: 'Production'
    AllowedHosts: app.properties.defaultHostName
    WEBSITE_RUN_FROM_PACKAGE: '1'
  }
}

resource ftpPolicy 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2024-04-01' = {
  parent: app
  name: 'ftp'
  properties: {
    allow: false
  }
}

resource scmPolicy 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2024-04-01' = {
  parent: app
  name: 'scm'
  properties: {
    allow: false
  }
}

output appResourceId string = app.id
output hostname string = app.properties.defaultHostName
