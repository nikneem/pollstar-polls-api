param defaultResourceName string
param location string
param storageAccountTables array
param containerVersion string

param integrationResourceGroupName string
param containerAppEnvironmentResourceName string
param applicationInsightsResourceName string

resource containerAppEnvironments 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: containerAppEnvironmentResourceName
  scope: resourceGroup(integrationResourceGroupName)
}
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsResourceName
  scope: resourceGroup(integrationResourceGroupName)
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-09-01' = {
  name: uniqueString(defaultResourceName)
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}
resource storageAccountTableService 'Microsoft.Storage/storageAccounts/tableServices@2021-09-01' = {
  name: 'default'
  parent: storageAccount
}
resource storageAccountTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2021-09-01' = [for table in storageAccountTables: {
  name: table
  parent: storageAccountTableService
}]
resource redisCache 'Microsoft.Cache/redis@2021-06-01' = {
  name: '${defaultResourceName}-cache'
  location: location
  properties: {
    sku: {
      name: 'Standard'
      capacity: 1
      family: 'C'
    }
    enableNonSslPort: false
    publicNetworkAccess: 'Enabled'
  }
}
resource webPubSub 'Microsoft.SignalRService/webPubSub@2021-10-01' = {
  name: '${defaultResourceName}-pubsub'
  location: location
  sku: {
    capacity: 1
    tier: 'Basic'
    name: 'Standard_S1'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
  }
  resource hub 'hubs' = {
    name: 'pollstar'
    properties: {
      anonymousConnectPolicy: 'allow'
    }
  }
}

resource apiContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: '${defaultResourceName}-aca'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnvironments.id

    configuration: {
      activeRevisionsMode: 'Single'
      secrets: [
        {
          name: 'storage-account-secret'
          value: listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value
        }
        {
          name: 'redis-cache-secret'
          value: listKeys(redisCache.id, redisCache.apiVersion).primaryKey
        }
        {
          name: 'web-pubsub-connectionstring'
          value: webPubSub.listKeys().primaryConnectionString

        }
        {
          name: 'application-insights-connectionstring'
          value: applicationInsights.properties.ConnectionString
        }
      ]
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
    }
    template: {
      containers: [
        {
          image: 'docker.io/nikneem/pollstar-api:${containerVersion}'
          name: 'pollstar-api'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'Cache__Secret'
              secretRef: 'redis-cache-secret'
            }
            {
              name: 'Cache__Endpoint'
              value: '${redisCache.name}.redis.cache.windows.net'
            }
            {
              name: 'Azure__StorageAccount'
              value: storageAccount.name
            }
            {
              name: 'Azure__StorageKey'
              secretRef: 'storage-account-secret'
            }
            {
              name: 'Azure__WebPubSub'
              secretRef: 'web-pubsub-connectionstring'
            }
            {
              name: 'Azure__PollStarHub'
              value: 'pollstar'
            }
            {
              name: 'APPLICATION_INSIGHTS_CONNECTIONSTRING'
              secretRef: 'application-insights-connectionstring'
            }
          ]

        }
      ]
      scale: {
        minReplicas: 2
        maxReplicas: 10
      }
    }
  }
}
