# Event Hubs Consumer and Producer Demo App

## Contents

1. Create an Event Hubs with Azure Resource Manager (ARM) template
1. Run Event Hubs Consumer (Subscriber) App
1. Run Event Hubs Producer (Publisher) App

## Mechanics

- Event Hubs Consumer App listen for events submitted to Event Hubs
- Event Hubs Producer App publishes events to Event Hubs with different parameters
- When events are submitted by the Producer App, you can see those events in the Consumer App

:::image type="content" source="media/Flow.png" alt-text="Flow":::

## Event Hubs Resource

In this demo, we will send and read/receive events from Event Hubs

Deploy Event Hubs by clicking "Deploy to Azure" button below :

[![Deploy](../media/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fdaisukeiot%2FAzure-IoT-Messaging%2Fmaster%2FEventHubs%2FEventHubsARM.json)

### Deploy Event Hubs

- Subscription  
    Select your Azure subscription
- Resource Group  
    Create a new resource group or use existing one
- Location  
    Select nearest Azure data center location
- Event Hubs Namespace  
    Name of Event Hubs Namespace.  Must be globally unique.  Use default name or provide your own.  
  - Default Value : [Resource Group Name]-NS-[Hash]

- Event Hub Name  
    Name of Event Hub.  Use default name or provide your own.

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

### Connection String for ListenRule

Retrieve Connection String of `ListenRule` from Event Hubs Namespace store to `listenCs` with :

- Windows

    ```cmd
    # A name of Resource Group
    set myResourceGroup=<Resource Group Name>

    # A name of Event Hubs Namespace
    set myNameSpace=<Event Hubs Namespace>

    # Select the Azure subscription that contains the resource group.
    az account set --subscription "<name or ID of the subscription>"

    # Get Connection String of ListenRule and store to listenCs
    for /f "tokens=*" %i in ('az eventhubs namespace authorization-rule keys list --resource-group %myResourceGroup% --namespace-name %myNameSpace% --name ListenRule --query primaryConnectionString') do set listenCs=%i
    ```

- Linux

    ```bash
    myResourceGroup=<Resource Group Name>
    myNameSpace=<Event Hubs Namespace>
    az account set --subscription "<name or ID of the subscription>"
    listenCS="$(az eventhubs namespace authorization-rule keys list --resource-group $myResourceGroup --namespace-name $myNameSpace --name SendRule --query primaryConnectionString --output tsv)"
    ```

#### ListenRule Example

Run AZ CLI command and set the Connection String to a local variable `listenCS`

- Windows

    ```cmd
    set myResourceGroup=EventHubs-Demo
    set myNameSpace=EventHubs-Demo-NS-i2bky5u4lygia
    az account set --subscription "HOL"
    for /f "tokens=*" %i in ('az eventhubs namespace authorization-rule keys list --resource-group %myResourceGroup% --namespace-name %myNameSpace% --name ListenRule --query primaryConnectionString') do set listenCs=%i
    ```

- Linux

    ```bash
    myResourceGroup=EventHubs-Demo
    myNameSpace=EventHubs-Demo-NS-i2bky5u4lygia
    az account set --subscription "HOL"
    listenCS="$(az eventhubs namespace authorization-rule keys list --resource-group $myResourceGroup --namespace-name $myNameSpace --name ListenRule --query primaryConnectionString --output tsv)"
    ```

### Retrieve Event Hub Name

Retrieve the name of Event Hub and set to `eventHub` variable with :

- Windows

    ```cmd
    for /f "tokens=*" %i in ('az eventhubs eventhub list --resource-group %myResourceGroup% --namespace-name %myNameSpace% --query [0].name') do set eventHub=%i
    ```

- Linux

    ```bash
    eventHub="$(az eventhubs eventhub list --resource-group $myResourceGroup --namespace-name $myNameSpace --query [0].name --output tsv)"
    ```

### Launch the Consumer App

Open a new CMD window then run the consumer app with :

- Windows

    ```cmd
    cd C:\Azure-IoT-Messaging\EventHubs\Consumer
    dotnet run -cs %listenCS% -cg democonsumergroup -hub %eventHub%
    ```

- Linux

    ```bash
        cd ~/Azure-IoT-Messaging/EventHubs/Consumer
        dotnet run -cs $listenCS -cg democonsumergroup -hub $eventHub
    ```

> [!TIP]
> Optionally, you can read all events in specified event hub with `-all` option

### Consumer App Example

```text
C:\Azure-IoT-Messaging\EventHubs\Consumer>dotnet run -cs %listenCS% -cg %myConsumerGroup% -hub %eventHub%
Connection String : Endpoint=sb://eventhubs-demo-ns-i2bky5u4lygia.serv....
Event Hub         : mydemoeventhub
Consumer Group    : democonsumergroup
Read all events   : False
Timeout           : 600 seconds
Read Start        : 3/27/2020 2:27:13 PM
```

## Producer App

The producer app sends events with following scenarios

|Scenario           |Description |Purpose  |
|--------------|-----------------------------------------|-------------------------|
|Single events |Sends multiple single events             | Events are sent to different partitions |
|Batched       |Sends multiple events in batch           | All events in a batch are sent to a single partition |
|Partition Key |Sends multiple events with Partition Key | All events with the same Partition key are sent to a single partition |
|Partition Id  |Sends multiple events with Partition Id  | The target partition can be specified by sender        |

### Connection String for SendRule and Event Hub name

Open a new CMD window then run the producer app.  

Retrieve Connection String of `SendRule` from Event Hubs Namespace and store to `sendCs` as well as Event Hub name and store to `eventHub` with :

- Windows

    ```cmd
    # A name of Resource Group
    set myResourceGroup=<Resource Group Name>

    # A name of Event Hubs Namespace
    set myNameSpace=<Event Hubs Namespace>

    # A name of Consumer Group
    set myConsumerGroup=<Consumer Group Name>
    # Select the Azure subscription that contains the resource group.
    az account set --subscription "<name or ID of the subscription>"

    # Get Connection String of ListenRule and store to sendCs
    for /f "tokens=*" %i in ('az eventhubs namespace authorization-rule keys list --resource-group %myResourceGroup% --namespace-name %myNameSpace% --name SendRule --query primaryConnectionString') do set sendCs=%i

    # Get Event Hub name and store to eventHub
    for /f "tokens=*" %i in ('az eventhubs eventhub list --resource-group %myResourceGroup% --namespace-name %myNameSpace% --query [0].name') do set eventHub=%i
    ```

- Linux

    ```bash
    myResourceGroup="<Resource Group Name>"
    myNameSpace="<Event Hubs Namespace>"
    myConsumerGroup="<Consumer Group Name>"
    az account set --subscription "<name or ID of the subscription>"
    listenCS="$(az eventhubs namespace authorization-rule keys list --resource-group $myResourceGroup --namespace-name $myNameSpace --name SendRule --query primaryConnectionString)"
    eventHub="$(az eventhubs eventhub list --resource-group $myResourceGroup --namespace-name $myNameSpace --query [0].name)"
    ```

#### Producer Parameter Example

- Windows

    ```cmd
    set myResourceGroup=EventHubs-Demo
    set myNameSpace=EventHubs-Demo-NS-i2bky5u4lygia
    az account set --subscription "HOL"
    for /f "tokens=*" %i in ('az eventhubs namespace authorization-rule keys list --resource-group %myResourceGroup% --namespace-name %myNameSpace% --name SendRule --query primaryConnectionString') do set sendCs=%i
    for /f "tokens=*" %i in ('az eventhubs eventhub list --resource-group %myResourceGroup% --namespace-name %myNameSpace% --query [0].name') do set eventHub=%i
    ```

- Linux

    ```bash
    myResourceGroup=EventHubs-Demo
    myNameSpace=EventHubs-Demo-NS-i2bky5u4lygia
    az account set --subscription "HOL"
    sendCs="$(az eventhubs namespace authorization-rule keys list --resource-group $myResourceGroup --namespace-name $myNameSpace --name SendRule --query primaryConnectionString)"
    eventHub="$(az eventhubs eventhub list --resource-group $myResourceGroup --namespace-name $myNameSpace --query [0].name)"
    ```

### Launch the Producer App

- Windows

    ```cmd
    cd C:\Azure-IoT-Messaging\EventHubs\Producer
    dotnet run -cs %sendCs% -hub %eventHub%
    ```

- Linux

```bash
    cd ~/Azure-IoT-Messaging/EventHubs/Producer
    dotnet run -cs $sendCs -hub $eventHub
```

#### Example

:::image type="content" source="media/VSCode.png" alt-text="VSCode":::

## Scenario 1 : No target partition

This scenario will result in events evenly distributed across partitions

> [!TIP]  
> Notice events are sent to partition 0 to 4 in round-robin fashion

```text
Enqueue at 2020/03/27 22:43:35:315 | Seq # 0238 | Partition 3 | Offset : 021184 | Message # 00
Enqueue at 2020/03/27 22:43:35:366 | Seq # 0359 | Partition 0 | Offset : 032336 | Message # 01
Enqueue at 2020/03/27 22:43:35:398 | Seq # 0696 | Partition 1 | Offset : 070464 | Message # 02
Enqueue at 2020/03/27 22:43:35:437 | Seq # 0328 | Partition 2 | Offset : 031744 | Message # 03
Enqueue at 2020/03/27 22:43:35:472 | Seq # 0239 | Partition 3 | Offset : 021248 | Message # 04
Enqueue at 2020/03/27 22:43:35:507 | Seq # 0360 | Partition 0 | Offset : 032400 | Message # 05
Enqueue at 2020/03/27 22:43:35:538 | Seq # 0697 | Partition 1 | Offset : 070528 | Message # 06
Enqueue at 2020/03/27 22:43:35:578 | Seq # 0329 | Partition 2 | Offset : 031808 | Message # 07
Enqueue at 2020/03/27 22:43:35:614 | Seq # 0240 | Partition 3 | Offset : 021312 | Message # 08
Enqueue at 2020/03/27 22:43:35:648 | Seq # 0361 | Partition 0 | Offset : 032464 | Message # 09
Enqueue at 2020/03/27 22:43:35:694 | Seq # 0698 | Partition 1 | Offset : 070592 | Message # 10
Enqueue at 2020/03/27 22:43:35:734 | Seq # 0330 | Partition 2 | Offset : 031872 | Message # 11
```

:::image type="content" source="media/VSCode-Output.png" alt-text="VSCode-Output":::

## Scenario 2 : Batch Mode

Since data is submitted in a batch (or in a single request), all events are stored in a single partition

> [!TIP]  
> Notice all events are sent to Partition 1

```text
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0241 | Partition 3 | Offset : 021376 | Message # 00 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0242 | Partition 3 | Offset : 021448 | Message # 01 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0243 | Partition 3 | Offset : 021520 | Message # 02 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0244 | Partition 3 | Offset : 021592 | Message # 03 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0245 | Partition 3 | Offset : 021664 | Message # 04 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0246 | Partition 3 | Offset : 021736 | Message # 05 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0247 | Partition 3 | Offset : 021808 | Message # 06 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0248 | Partition 3 | Offset : 021880 | Message # 07 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0249 | Partition 3 | Offset : 021952 | Message # 08 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0250 | Partition 3 | Offset : 022024 | Message # 09 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0251 | Partition 3 | Offset : 022096 | Message # 10 Batch 00
Enqueue at 2020/03/27 22:44:14:596 | Seq # 0252 | Partition 3 | Offset : 022168 | Message # 11 Batch 00
Enqueue at 2020/03/27 22:44:14:631 | Seq # 0362 | Partition 0 | Offset : 032528 | Message # 00 Batch 01
Enqueue at 2020/03/27 22:44:14:631 | Seq # 0363 | Partition 0 | Offset : 032600 | Message # 01 Batch 01
Enqueue at 2020/03/27 22:44:14:631 | Seq # 0364 | Partition 0 | Offset : 032672 | Message # 02 Batch 01
Enqueue at 2020/03/27 22:44:14:631 | Seq # 0365 | Partition 0 | Offset : 032744 | Message # 03 Batch 01
Enqueue at 2020/03/27 22:44:14:631 | Seq # 0366 | Partition 0 | Offset : 032816 | Message # 04 Batch 01
Enqueue at 2020/03/27 22:44:14:631 | Seq # 0367 | Partition 0 | Offset : 032888 | Message # 05 Batch 01
Enqueue at 2020/03/27 22:44:14:631 | Seq # 0368 | Partition 0 | Offset : 032960 | Message # 06 Batch 01
  :
```

## Scenario 3 : With Partition Key

A partition key is used as a hash and Event Hubs keeps events with the same hash to be stored in a single partition

> [!TIP]  
> Notice all events are sent to Partition 2  
> This makes easier to process events in order  
> However, you don't know which partition your events will end up

Example :

Key `GJMD5Z` => Partition 0
Key `CysOuc` => Partition 1
Key `v4myXj` => Partition 3

```text
Enqueue at 2020/03/27 22:44:46:547 | Seq # 0374 | Partition 0 | Offset : 033392 | Message # 00 with Key GJMD5Z
Enqueue at 2020/03/27 22:44:46:579 | Seq # 0711 | Partition 1 | Offset : 071520 | Message # 01 with Key CysOuc
Enqueue at 2020/03/27 22:44:46:609 | Seq # 0375 | Partition 0 | Offset : 033504 | Message # 02 with Key GJMD5Z
Enqueue at 2020/03/27 22:44:46:650 | Seq # 0253 | Partition 3 | Offset : 022240 | Message # 03 with Key Krd7xK
Enqueue at 2020/03/27 22:44:46:697 | Seq # 0254 | Partition 3 | Offset : 022352 | Message # 04 with Key Krd7xK
Enqueue at 2020/03/27 22:44:46:728 | Seq # 0255 | Partition 3 | Offset : 022464 | Message # 05 with Key v4myXj
Enqueue at 2020/03/27 22:44:46:766 | Seq # 0712 | Partition 1 | Offset : 071632 | Message # 06 with Key CysOuc
Enqueue at 2020/03/27 22:44:46:791 | Seq # 0256 | Partition 3 | Offset : 022576 | Message # 07 with Key v4myXj
Enqueue at 2020/03/27 22:44:46:829 | Seq # 0713 | Partition 1 | Offset : 071744 | Message # 08 with Key CysOuc
Enqueue at 2020/03/27 22:44:46:860 | Seq # 0714 | Partition 1 | Offset : 071856 | Message # 09 with Key CysOuc
Enqueue at 2020/03/27 22:44:46:900 | Seq # 0257 | Partition 3 | Offset : 022688 | Message # 10 with Key Krd7xK
Enqueue at 2020/03/27 22:44:46:923 | Seq # 0715 | Partition 1 | Offset : 071968 | Message # 11 with Key CysOuc
Enqueue at 2020/03/27 22:44:46:962 | Seq # 0258 | Partition 3 | Offset : 022800 | Message # 12 with Key Krd7xK
Enqueue at 2020/03/27 22:44:47:009 | Seq # 0259 | Partition 3 | Offset : 022912 | Message # 13 with Key v4myXj
  :
```

## Scenario 4 : With Partition Id

You may send events with specific partition with `Partition Id`

> [!TIP]  
> Notice all events are sent to respective partition as specified  
> With this, you have full control over which partition you need to listen to

```bash
Enqueue at 2020/03/27 22:44:48:438 | Seq # 0383 | Partition 0 | Offset : 034400 | Message # 00 to Partition 0
Enqueue at 2020/03/27 22:44:48:469 | Seq # 0384 | Partition 0 | Offset : 034480 | Message # 01 to Partition 0
Enqueue at 2020/03/27 22:44:48:500 | Seq # 0385 | Partition 0 | Offset : 034560 | Message # 02 to Partition 0
Enqueue at 2020/03/27 22:44:48:627 | Seq # 0724 | Partition 1 | Offset : 072976 | Message # 00 to Partition 1
Enqueue at 2020/03/27 22:44:48:658 | Seq # 0725 | Partition 1 | Offset : 073056 | Message # 01 to Partition 1
Enqueue at 2020/03/27 22:44:48:705 | Seq # 0726 | Partition 1 | Offset : 073136 | Message # 02 to Partition 1
Enqueue at 2020/03/27 22:44:48:801 | Seq # 0343 | Partition 2 | Offset : 032800 | Message # 00 to Partition 2
Enqueue at 2020/03/27 22:44:48:848 | Seq # 0344 | Partition 2 | Offset : 032880 | Message # 01 to Partition 2
Enqueue at 2020/03/27 22:44:48:879 | Seq # 0345 | Partition 2 | Offset : 032960 | Message # 02 to Partition 2
Enqueue at 2020/03/27 22:44:48:994 | Seq # 0279 | Partition 3 | Offset : 025152 | Message # 00 to Partition 3
Enqueue at 2020/03/27 22:44:49:025 | Seq # 0280 | Partition 3 | Offset : 025232 | Message # 01 to Partition 3
Enqueue at 2020/03/27 22:44:49:056 | Seq # 0281 | Partition 3 | Offset : 025312 | Message # 02 to Partition 3
```

## Cleanup

To clean up resources, run

- Windows

    ```cmd
    az group delete --name %myResourceGroup% --yes
    ```

- Linux

    ```bash
    az group delete --name $myResourceGroup --yes
    ```
    