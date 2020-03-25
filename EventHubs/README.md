# Event Hubs Consumer and Producer Demo App

## Contents

1. Azure Resource Manager (ARM) template to create Event Hubs
1. Event Hubs Consumer (Subscriber) App
1. Event Hubs Producer (Publisher) App

## Event Hubs Resource

In this demo, we will send and read/receive events from Event Hubs

Deploy Event Hubs by clicking "Deploy to Azure" button :

[![Deploy](../media/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fdaisukeiot%2FAzure-IoT-Messaging%2Fmaster%2FEventHubs%2FEventHubsARM.json)

### Deploy Event Hubs

- Subscription  
    Select your Azure subscription
- Resource Group  
    Create a new resource group or use existing one
- Location  
    Select nearest Azure data center location
- Event Hubs Namespace  
    Name of Event Hubs Namespace
- Event Hub Name  
    Name of Event Hub

:::image type="content" source="media/ARM-01.png" alt-text="ARM-01":::

### Event Hubs Setting

The ARM Template will create Event Hubs with following settings

|Name                 |Setting     |Description  |
|---------------------|------------|-------------------------|
|Namespace Name       | From UI    | Must be globally unique |
|Event Hub Name       | From UI    | |
|Location             | From UI    | |
|SKU                  | Standard   | |
|Throughput Unit      | 1          | |
|Partitions           | 4          | Partition IDs = 0,1,2,3       |
|Shared Access Policy | SendRule   | Only send events permission   |
|Shared Access Policy | ListenRule | Only listen events permission |
|Auto Inflate         | No         | |
|Kafka Support        | Yes        | |
|Message Retention    | 1 day      | |
|Capture              | Not Enabled| |
|Consumer Group       | democonsumergroup | |

## Consumer App

To run the Consumer App you need :

1. Connection String
1. Consumer Group : `democonsumergroup`
1. Event Hub Name

### Connection String for SendRule

Retrieve Connection String of `ListenRule` from Event Hubs Namespace with :

```bash
# A name of existing resource group you created earlier
myResourceGroup="<resource group name>"

# A name of Event Hubs Namespace you created earlier
myNameSpace="<Event Hubs Namespace>"

# Select the Azure subscription that contains the resource group.
az account set --subscription "<name or ID of the subscription>"

# Get resource ID of the resource group.
az eventhubs namespace authorization-rule keys list --resource-group $myResourceGroup --namespace-name $myNameSpace --name SendRule --query primaryConnectionString
```

Example :

```bash
myResourceGroup="EventHubs-Demo"
myNameSpace="EventHubsDemoNamespace"
az eventhubs namespace authorization-rule keys list --resource-group $myResourceGroup --namespace-name $myNameSpace --name SendRule --query primaryConnectionString
"Endpoint=sb://eventhubsdemonamespace.servicebus.windows.net/;SharedAccessKeyName=SendRule;SharedAccessKey=JTEfH7JR+d/kCpgcgyzYwVzb445DQSVPHqyLEZ9BSqE="
```

### Launch the Consumer App

Open a new CMD window then run the consumer app with :

```CMD
cd \Azure-IoT-Messaging
dotnet --project .\Consumer\Consumer.csproj -cs <Connection String> -cg democonsumergroup -hub <Event Hub Name>
```

> [!TIP]
> Optionally, you can read all events in specified event hub with `-all` option

## Producer App

The producer app sends events with following scenarios

|Scenario           |Description |Purpose  |
|--------------|-----------------------------------------|-------------------------|
|Single events |Sends multiple single events             | Events are sent to different partitions |
|Batched       |Sends multiple events in batch           | All events in a batch are sent to a single partition |
|Partition Key |Sends multiple events with Partition Key | All events with the same Partition key are sent to a single partition |
|Partition Id  |Sends multiple events with Partition Id  | The target partition can be specified by sender        |

### Connection String for ListenRule

The ARM Template creates `ListenRule` with Listen permission.  Retrieve Connection String for the rule with :

```bash
# Get resource ID of the resource group.
az eventhubs namespace authorization-rule keys list --resource-group $myResourceGroup --namespace-name $myNameSpace --name ListenRule --query primaryConnectionString
```
Example :

```bash
az eventhubs namespace authorization-rule keys list --resource-group $myResourceGroup --namespace-name $myNameSpace --name ListenRule --query primaryConnectionString
"Endpoint=sb://eventhubsdemonamespace.servicebus.windows.net/;SharedAccessKeyName=ListenRule;SharedAccessKey=iUFnJUIu1I1/ILT0oaIYHrtvJHTkPEyizirnu5cQclw="
```

### Launch the Producer App

Open a new CMD window then run the producer app with :

```powershell
dotnet --project .\Producer\Producer.csproj -cs <Connection String> -hub <Event Hub Name>
```

## Scenario 1 : No target partition

This scenario will result in events evenly distributed across partitions

> [!TIP]  
> Notice events are sent to partition 0 to 4 in round-robin fashion

```bash
Enqueue at 2020/03/24 11:30:08:995 | Seq # 0103 | Partition 1 | Offset : 006120 | Data Message # 00
Enqueue at 2020/03/24 11:30:09:137 | Seq # 0176 | Partition 2 | Offset : 015680 | Data Message # 01
Enqueue at 2020/03/24 11:30:09:334 | Seq # 0104 | Partition 3 | Offset : 006272 | Data Message # 02
Enqueue at 2020/03/24 11:30:09:513 | Seq # 0104 | Partition 0 | Offset : 006176 | Data Message # 03
Enqueue at 2020/03/24 11:30:09:588 | Seq # 0104 | Partition 1 | Offset : 006176 | Data Message # 04
Enqueue at 2020/03/24 11:30:09:637 | Seq # 0177 | Partition 2 | Offset : 015744 | Data Message # 05
Enqueue at 2020/03/24 11:30:09:663 | Seq # 0105 | Partition 3 | Offset : 006328 | Data Message # 06
Enqueue at 2020/03/24 11:30:09:779 | Seq # 0105 | Partition 0 | Offset : 006232 | Data Message # 07
Enqueue at 2020/03/24 11:30:09:917 | Seq # 0105 | Partition 1 | Offset : 006232 | Data Message # 08
Enqueue at 2020/03/24 11:30:09:949 | Seq # 0178 | Partition 2 | Offset : 015808 | Data Message # 09
Enqueue at 2020/03/24 11:30:09:975 | Seq # 0106 | Partition 3 | Offset : 006384 | Data Message # 10
Enqueue at 2020/03/24 11:30:10:013 | Seq # 0106 | Partition 0 | Offset : 006288 | Data Message # 11
```

## Scenario 2 : Batch Mode

Since data is submitted in a batch (or in a single request), all events are stored in a single partition

> [!TIP]  
> Notice all events are sent to Partition 1

```bash
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0106 | Partition 1 | Offset : 006288 | Data Message # 00 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0107 | Partition 1 | Offset : 006352 | Data Message # 01 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0108 | Partition 1 | Offset : 006416 | Data Message # 02 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0109 | Partition 1 | Offset : 006480 | Data Message # 03 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0110 | Partition 1 | Offset : 006544 | Data Message # 04 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0111 | Partition 1 | Offset : 006608 | Data Message # 05 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0112 | Partition 1 | Offset : 006672 | Data Message # 06 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0113 | Partition 1 | Offset : 006736 | Data Message # 07 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0114 | Partition 1 | Offset : 006800 | Data Message # 08 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0115 | Partition 1 | Offset : 006864 | Data Message # 09 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0116 | Partition 1 | Offset : 006928 | Data Message # 10 Batched
Enqueue at 2020/03/24 11:32:33:007 | Seq # 0117 | Partition 1 | Offset : 006992 | Data Message # 11 Batched
```

## Scenario 3 : With Partition Key

A partition key is used as a hash and Event Hubs keeps events with the same hash to be stored in a single partition

> [!TIP]  
> Notice all events are sent to Partition 2  
> This makes easier to process events in order  
> However, you don't know which partition your events will end up

```bash
Enqueue at 2020/03/24 11:33:30:988 | Seq # 0179 | Partition 2 | Offset : 015872 | Data Message # 00 with Partition Key
Enqueue at 2020/03/24 11:33:31:332 | Seq # 0180 | Partition 2 | Offset : 015992 | Data Message # 01 with Partition Key
Enqueue at 2020/03/24 11:33:31:722 | Seq # 0181 | Partition 2 | Offset : 016112 | Data Message # 02 with Partition Key
Enqueue at 2020/03/24 11:33:32:003 | Seq # 0182 | Partition 2 | Offset : 016232 | Data Message # 03 with Partition Key
Enqueue at 2020/03/24 11:33:32:285 | Seq # 0183 | Partition 2 | Offset : 016352 | Data Message # 04 with Partition Key
Enqueue at 2020/03/24 11:33:32:628 | Seq # 0184 | Partition 2 | Offset : 016472 | Data Message # 05 with Partition Key
Enqueue at 2020/03/24 11:33:32:972 | Seq # 0185 | Partition 2 | Offset : 016592 | Data Message # 06 with Partition Key
Enqueue at 2020/03/24 11:33:33:176 | Seq # 0186 | Partition 2 | Offset : 016712 | Data Message # 07 with Partition Key
Enqueue at 2020/03/24 11:33:33:411 | Seq # 0187 | Partition 2 | Offset : 016832 | Data Message # 08 with Partition Key
Enqueue at 2020/03/24 11:33:33:536 | Seq # 0188 | Partition 2 | Offset : 016952 | Data Message # 09 with Partition Key
Enqueue at 2020/03/24 11:33:33:614 | Seq # 0189 | Partition 2 | Offset : 017072 | Data Message # 10 with Partition Key
Enqueue at 2020/03/24 11:33:33:676 | Seq # 0190 | Partition 2 | Offset : 017192 | Data Message # 11 with Partition Key
```

## Scenario 4 : With Partition Id

You may send events with specific partition with `Partition Id`

> [!TIP]  
> Notice all events are sent to respective partition as specified  
> With this, you have full control over which partition you need to listen to

```bash
Enqueue at 2020/03/24 11:33:33:852 | Seq # 0107 | Partition 0 | Offset : 006344 | Data Message # 00 to Partition 0
Enqueue at 2020/03/24 11:33:33:898 | Seq # 0108 | Partition 0 | Offset : 006416 | Data Message # 01 to Partition 0
Enqueue at 2020/03/24 11:33:33:945 | Seq # 0109 | Partition 0 | Offset : 006488 | Data Message # 02 to Partition 0
Enqueue at 2020/03/24 11:33:34:023 | Seq # 0110 | Partition 0 | Offset : 006560 | Data Message # 03 to Partition 0
Enqueue at 2020/03/24 11:33:34:155 | Seq # 0118 | Partition 1 | Offset : 007056 | Data Message # 00 to Partition 1
Enqueue at 2020/03/24 11:33:34:186 | Seq # 0119 | Partition 1 | Offset : 007128 | Data Message # 01 to Partition 1
Enqueue at 2020/03/24 11:33:34:233 | Seq # 0120 | Partition 1 | Offset : 007200 | Data Message # 02 to Partition 1
Enqueue at 2020/03/24 11:33:34:264 | Seq # 0121 | Partition 1 | Offset : 007272 | Data Message # 03 to Partition 1
Enqueue at 2020/03/24 11:33:34:365 | Seq # 0191 | Partition 2 | Offset : 017312 | Data Message # 00 to Partition 2
Enqueue at 2020/03/24 11:33:34:427 | Seq # 0192 | Partition 2 | Offset : 017392 | Data Message # 01 to Partition 2
Enqueue at 2020/03/24 11:33:34:552 | Seq # 0193 | Partition 2 | Offset : 017472 | Data Message # 02 to Partition 2
Enqueue at 2020/03/24 11:33:34:631 | Seq # 0194 | Partition 2 | Offset : 017552 | Data Message # 03 to Partition 2
Enqueue at 2020/03/24 11:33:34:831 | Seq # 0107 | Partition 3 | Offset : 006440 | Data Message # 00 to Partition 3
Enqueue at 2020/03/24 11:33:34:862 | Seq # 0108 | Partition 3 | Offset : 006512 | Data Message # 01 to Partition 3
Enqueue at 2020/03/24 11:33:34:909 | Seq # 0109 | Partition 3 | Offset : 006584 | Data Message # 02 to Partition 3
Enqueue at 2020/03/24 11:33:34:940 | Seq # 0110 | Partition 3 | Offset : 006656 | Data Message # 03 to Partition 3
```

## Cleanup

To clean up resources, run

```bash
az group delete --name $myResourceGroup --yes
```
