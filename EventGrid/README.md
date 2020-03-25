# Event Grid Publisher and Subscriber Demo

## Contents

1. Azure Resource Manager (ARM) template to create a web site as WebHooks subscriber app
1. A script to create a subscription with AzCli

## WebHooks Subscriber App

Using Event Grid Viewer app from <https://github.com/Azure-Samples/azure-event-grid-viewer>, set up a web site by clicking "Deploy to Azure" button :

[![Deploy](../media/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Fazure-event-grid-viewer%2Fmaster%2Fazuredeploy.json)

### Deploy Event Grid Viewer Web App

The ARM template will deploy `App Service` and `App Service Plan` with following parameters

- Subscription  
    Select your Azure subscription
- Resource Group  
    Create a new resource group or use existing one
- Location  
    Select nearest Azure data center location
- Site Name : Becomes your website's sub-domain  
    e.g. <http://[Site-Name].azurewebsites.net>  
    Must be globally unique
- Hosting Plan Name  
    A name for App Service
- SKU : F1 is fine, if F1 is already used, D1 ($9.49/month)

:::image type="content" source="media/ARM-01.png" alt-text="ARM-01":::

Once deployed, you can access the web site with <http://[Site_Name].azurewebsites.net>

:::image type="content" source="media/WebApp-01.png" alt-text="WebApp-01":::

## Setting up publishers

Configure Azure Resource Group as a publisher

### Install Azure CLI Event Grid extension

Install Event Grid extension with :

```bash
az extension add --name eventgrid
```

### Register a subscription

```bash
# Endpoint is your WebHooks app
# e.g. http://eventgridviewer.azurewebsites.net/api/updates
myEndpoint="<endpoint URL>"

# A name of existing resource group
# You can use the resource group from Event Grid Viewer
myResourceGroup="<resource group name>"

# Select the Azure subscription that contains the resource group.
az account set --subscription "<name or ID of the subscription>"

# Get resource ID of the resource group.
resourceGroupID=$(az group show --name $myResourceGroup --query id --output tsv)

# Subscribe to the resource group. Provide the name of the resource group you want to subscribe to.
az eventgrid event-subscription create \
  --name demoSubscriptionToResourceGroup \
  --source-resource-id $resourceGroupID \
  --endpoint $myEndpoint
```

This will create an event subscription called `demoSubscriptionToResourceGroup`

:::image type="content" source="media/ARM-02.png" alt-text="ARM-02":::

### Event Types

Resource Group generates events from Azure Resource Manager on various events such as when a new resource is created or deleted.

Full list of Event Types from Azure Resource Manager :

https://docs.microsoft.com/en-us/azure/event-grid/event-schema-resource-groups#available-event-types

### Receiving Events

When you create or delete a resource, you should see events in Event Grid Viewer app

:::image type="content" source="media/WebApp-02.png" alt-text="WebApp-02":::

### Create a resource

Let's create a resource in the resource group so you can see events in Event Grid Viewer

Create Device Provisioning Service (DPS) with :

```bash
az iot dps create --resource-group $myResourceGroup --name EventGridViewerDPS
```

Example :

```bash
az iot hub create --name EventGridViewerTestIoTHub --resource-group $myResourceGroup --sku S1
{
  "etag": "AAAAAAxJUoM=",
  "id": "/subscriptions/8fe26a8e-ded0-4dce-83c7-efdb495f2dc3/resourceGroups/EventGrid-Viewer/providers/Microsoft.Devices/IotHubs/EventGridViewerTestIoTHub",
  "location": "westus2",
  "name": "EventGridViewerTestIoTHub",
  "properties": {
    "authorizationPolicies": null,
    "cloudToDevice": {
      "defaultTtlAsIso8601": "1:00:00",
      "feedback": {
        "lockDurationAsIso8601": "0:00:05",
        "maxDeliveryCount": 10,
        "ttlAsIso8601": "1:00:00"
      },
      "maxDeliveryCount": 10
    },
    "comments": null,
    "deviceStreams": null,
    "enableFileUploadNotifications": false,
    "eventHubEndpoints": {
      "events": {
        "endpoint": "sb://iothub-ns-eventgridv-3136514-74686ad0dc.servicebus.windows.net/",
        "partitionCount": 4,
        "partitionIds": [
          "0",
          "1",
          "2",
          "3"
        ],
        "path": "eventgridviewertestiothub",
        "retentionTimeInDays": 1
      }
    },
    "features": "None",
    "hostName": "EventGridViewerTestIoTHub.azure-devices.net",
    "ipFilterRules": [],
    "locations": [
      {
        "location": "West US 2",
        "role": "primary"
      },
      {
        "location": "West Central US",
        "role": "secondary"
      }
    ],
    "messagingEndpoints": {
      "fileNotifications": {
        "lockDurationAsIso8601": "0:01:00",
        "maxDeliveryCount": 10,
        "ttlAsIso8601": "1:00:00"
      }
    },
    "provisioningState": "Succeeded",
    "routing": {
      "endpoints": {
        "eventHubs": [],
        "serviceBusQueues": [],
        "serviceBusTopics": [],
        "storageContainers": []
      },
      "enrichments": null,
      "fallbackRoute": {
        "condition": "true",
        "endpointNames": [
          "events"
        ],
        "isEnabled": true,
        "name": "$fallback"
      },
      "routes": []
    },
    "state": "Active",
    "storageEndpoints": {
      "$default": {
        "connectionString": "",
        "containerName": "",
        "sasTtlAsIso8601": "1:00:00"
      }
    }
  },
  "resourcegroup": "EventGrid-Viewer",
  "sku": {
    "capacity": 1,
    "name": "S1",
    "tier": "Standard"
  },
  "subscriptionid": "8fe26a8e-ded0-4dce-83c7-efdb495f2dc3",
  "tags": {},
  "type": "Microsoft.Devices/IotHubs"
}
```

You should see new events in Event Grid Viewer

:::image type="content" source="media/WebApp-03.png" alt-text="WebApp-03":::

## Other Azure services as publishers

|Service                |Link     |
|-----------------------|---------|
|App Configuration      | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-app-configuration> |
|Azure Machine Learning | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-machine-learning>|
|Azure Maps             | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-azure-maps> |
|Azure SingalR          | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-azure-signalr>|
|Blob Storage           | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-blob-storage>         |
|Container Registry     | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-container-registry>         |
|Event Hubs             | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-event-hubs>         |
|IoT Hub                | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-iot-hub> |
|Key Vault              | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-key-vault>|
|Media Services         | <https://docs.microsoft.com/en-us/azure/media-services/latest/media-services-event-schemas?toc=/azure/event-grid/toc.json>|
|Resource Group         | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-resource-groups>        |
|Service Bus            | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-service-bus> |
|Subscription           | <https://docs.microsoft.com/en-us/azure/event-grid/event-schema-subscriptions>         |


## Clean Up

Clean up the resource group you created from this demo with :

> [!CAUTION]  
> This will also delete Event Viewer Grid Viewer

```bash
az group delete --name $myResourceGroup --yes
```
