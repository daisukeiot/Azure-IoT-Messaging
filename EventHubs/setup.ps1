#
# Modify parameters below
#
$location          = "westus2"
$subscription      = ""

$resourceGroup     = "EventHubsDemo"
$eventHubNamespace = $resourceGroup + "-NS-" + "{0:X}" -f (Get-Random)
$eventHubName      = $resourceGroup + "-EH-" + "{0:X}" -f (Get-Random)
$consumerGroupName = "DemoConsumerGroup"

function log {
    Param(
        [Parameter(Mandatory)]
        [string]$logMessage
    )
    Write-Host "$(get-date -Format yy/MM/dd-HH:mm:ss) | $($logMessage)"
}


$ErrorActionPreference = "SilentlyContinue"
Set-Item Env:\SuppressAzurePowerShellBreakingChangeWarnings "true"

Write-Host "=============================================================================================="

# Install modules

$module = Get-InstalledModule -Name Az

if ($null -eq $module)
{
    log "Installing Az Module"
    Install-Module -Name Az -AllowClobber -Scope CurrentUser -Force
}

# Login to Azure subscription if not logged in
$azAccount = Get-AzContext
if ($null -eq $azAccount)
{
    (New-Object -Com Shell.Application).Open("https://microsoft.com/devicelogin")
    $azAccount = Connect-AzAccount -Scope CurrentUser -Subscription $subscription -UseDeviceAuthentication
}

log "Subscription used             : $($azAccount.Subscription.Name)"

#
# Create a resource group
# https://docs.microsoft.com/en-us/powershell/module/az.eventhub/new-azeventhub?view=azps-3.6.1
#

#
# Make sure the resource group does not exist
#
$resGroup = Get-AzResourceGroup -Name $resourceGroup

if ($resGroup)
{
    Write-Host "Resource Group $($resourceGroup) already exist"
    $question = 'Are you sure you want to proceed?'
    $choices  = '&Yes', '&No'

    $decision = $Host.UI.PromptForChoice("Warning", $question, $choices, 1)
    if ($decision -eq 1) {
        exit(1)
    }
} else {
    log "Creating Resource Group       : $($resourceGroup) @ $($location)"
    $resGroup = New-AzResourceGroup -Name $resourceGroup -Location $location
}

#
# Create an Event Hubs namespace
# https://docs.microsoft.com/en-us/powershell/module/az.eventhub/new-azeventhubnamespace?view=azps-3.6.1
#
log "Creating Event Hubs Namespace : $($eventHubNamespace)"
$nameSpace = New-AzEventHubNamespace -ResourceGroupName $resourceGroup -Name $eventHubNamespace -Location $location -Confirm:$false

#
# Create an Event Hub
# https://docs.microsoft.com/en-us/powershell/module/az.eventhub/new-azeventhub?view=azps-3.6.1
#
log "Creating Event Hub            : $($eventHubName)"
$eventHub = New-AzEventHub -ResourceGroupName $resourceGroup -Namespace $eventHubNamespace -Name $eventHubName -messageRetentionInDays 1

#
# Create Consumer Group
# https://docs.microsoft.com/en-us/powershell/module/az.eventhub/New-AzEventHubConsumerGroup?view=azps-3.6.1
#
log "Creating Consumer Group       : $($consumerGroup)"
$consumerGroup = New-AzEventHubConsumerGroup -ResourceGroupName $resourceGroup -Namespace $eventHubNamespace -EventHub $eventHubName -Name $consumerGroupName

#
# Create a new key to send and listen
# https://docs.microsoft.com/en-us/powershell/module/az.eventhub/new-azeventhubauthorizationrule?view=azps-3.6.1
#
$sendRule = New-AzEventHubAuthorizationRule -ResourceGroupName $resourceGroup -Namespace $eventHubNamespace -Name "SendRule" -Rights @("Send")
$listenRule = New-AzEventHubAuthorizationRule -ResourceGroupName $resourceGroup -Namespace $eventHubNamespace -Name "ListenRule" -Rights @("Listen")

#
# Get the connection string required to connect the clients to your event hub
# https://docs.microsoft.com/en-us/powershell/module/az.eventhub/get-azeventhubkey?view=azps-3.6.1
#
$sendKey = Get-AzEventHubKey -ResourceGroupName $resourceGroup -Namespace $eventHubNamespace -Name "SendRule"
$listenKey = Get-AzEventHubKey -ResourceGroupName $resourceGroup -Namespace $eventHubNamespace -Name "ListenRule"

if (($null -eq $sendKey) -or ($null -eq $listenKey))
{
    Write-Error "Could not retrieve Connection Strings"
    exit(1)
}

Write-Host '    ______                 __     __  __      __        '
Write-Host '   / ____/   _____  ____  / /_   / / / /_  __/ /_  _____'
Write-Host '  / __/ | | / / _ \/ __ \/ __/  / /_/ / / / / __ \/ ___/'
Write-Host ' / /___ | |/ /  __/ / / / /_   / __  / /_/ / /_/ (__  )'
Write-Host '/_____/ |___/\___/_/ /_/\__/  /_/ /_/\__,_/_.___/____/'
Write-Host ''
Write-Host "Namespace -------------------------------------------------------"
Write-host "  Namespace         : $($nameSpace.Name)"
Write-host "  Resource Group    : $($nameSpace.ResourceGroupName)"
Write-host "  Location          : $($nameSpace.Location)"
Write-host "  SKU               : $($nameSpace.Sku)"
Write-Host "  AutoFlate         : $($nameSpace.IsAutoInflateEnabled)"
Write-Host "  Kafka             : $($nameSpace.KafkaEnabled)"
Write-Host "Access Plicy ----------------------------------------------------"

$accessPolicies = Get-AzEventHubAuthorizationRule -ResourceGroupName $resourceGroup -Namespace $eventHubNamespace

foreach ($accessPolicy in $accessPolicies)
{
    Write-Host "  Policy Name       : $($accessPolicy.Name)"
    Write-Host "    Rights          : $($accessPolicy.Rights)"
}

Write-Host "Event Hub -------------------------------------------------------"
Write-host "  Hub Name          : $($eventHub.Name)"
Write-host "  Message Retention : $($eventHub.MessageRetentionInDays) day(s)"
Write-host "  Partition Count   : $($eventHub.PartitionCount)"
Write-host "  Partition ID(s)   : $($eventHub.PartitionIds)"
Write-Host "  Consumer Group    : $($consumerGroup.Name)"

Write-Host "=============================================================================================="
Write-Host '   ______ '                                         
Write-Host '  / ____/___  ____  _______  ______ ___  ___  _____'
Write-Host ' / /   / __ \/ __ \/ ___/ / / / __ `__ \/ _ \/ ___/'
Write-Host '/ /___/ /_/ / / / (__  ) /_/ / / / / / /  __/ /'    
Write-Host '\____/\____/_/ /_/____/\__,_/_/ /_/ /_/\___/_/'    
Write-Host "=============================================================================================="
Write-Host "Run Consumer app with"
Write-Host ""
Write-Host "start cmd /k dotnet run --project .\Consumer\Consumer.csproj -cs `"$($listenKey.PrimaryConnectionString)`" -cg $($consumerGroup.Name) -hub $($eventHub.Name)"
Write-Host ""
Write-Host '    ____                 __                    '
Write-Host '   / __ \_________  ____/ /_  __________  _____'
Write-Host '  / /_/ / ___/ __ \/ __  / / / / ___/ _ \/ ___/'
Write-Host ' / ____/ /  / /_/ / /_/ / /_/ / /__/  __/ /    '
Write-Host '/_/   /_/   \____/\__,_/\__,_/\___/\___/_/     '
Write-Host "=============================================================================================="
Write-Host "Then run producer app with"
Write-Host ""
Write-Host "start cmd /k dotnet run --project .\Producer\Producer.csproj -cs `"$($sendKey.PrimaryConnectionString)`" -hub $($eventHub.Name)"
