﻿using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.CommandLineUtils;

namespace Consumer
{
    class Consumer
    {
        private static int readTimetoutSeconds = 600;
        static async Task<int> Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.FullName = "Event Hubs Consumer";
            app.Description = "Event Hubs Consumer Sample";
            app.HelpOption("-h|-?|--help");
            var hubName = String.Empty;
            var connectionString = app.Option("-cs", "Specify ConnectionString", CommandOptionType.SingleValue, true);
            var group            = app.Option("-cg", "Specify Consumer Group", CommandOptionType.SingleValue);
            var hub              = app.Option("-hub", "Specify Event Hub", CommandOptionType.SingleValue);
            var allEvents        = app.Option("-all", "Read all events", CommandOptionType.NoValue);
            app.Execute(args);

            if (!connectionString.HasValue() || string.IsNullOrEmpty(connectionString.Value()))
            {
                ShowHelp();
                return 0;
            }

            var csValue = connectionString.Value().Replace("\"", "").Trim();
            var csValueItems = csValue.Split(";");

            foreach (var csValueItem in csValueItems)
            {
                var valuePair = csValueItem.Split("=");

                if (valuePair[0].ToLower() == "entitypath")
                {
                    hubName = valuePair[1];
                    break;
                }
            }

            if (hubName == String.Empty)
            {
                if (!hub.HasValue() || string.IsNullOrEmpty(hub.Value()))
                {
                    ShowHelp();
                    return 0;
                } else {
                    hubName = hub.Value().Replace("\"", "").Trim();
                }
            }

            //
            // https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.consumer.eventhubconsumerclient?view=azure-dotnet
            // Select Consumer Group
            //
            string consumerGroup;

            if (group.HasValue() && !string.IsNullOrEmpty(group.Value()))
            {
                consumerGroup = group.Value().Replace("\"", "").Trim();
            }
            else
            {
                consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
            }

            Console.WriteLine("Connection String : {0}....", connectionString.Value().Replace("\"", "").Trim().Substring(0, 50));
            Console.WriteLine($"Event Hub         : {hubName}");
            Console.WriteLine($"Consumer Group    : {consumerGroup}");
            Console.WriteLine($"Read all events   : {allEvents.HasValue()}");
            Console.WriteLine($"Timeout           : {readTimetoutSeconds} seconds");

            EventHubConsumerClient consumer = new EventHubConsumerClient(consumerGroup, connectionString.Value().Replace("\"", "").Trim(), hubName);

            var part = await consumer.GetPartitionIdsAsync();
            var part_prop = await consumer.GetPartitionPropertiesAsync(part[0]);
            var hub_prop = await consumer.GetEventHubPropertiesAsync();

            using CancellationTokenSource cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(TimeSpan.FromSeconds(readTimetoutSeconds));

            //
            // https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.consumer.eventhubconsumerclient.readeventsasync?view=azure-dotnet
            //
            // Timeout 10 Min
            //
            ReadEventOptions readOptions = new ReadEventOptions
            {
                MaximumWaitTime = TimeSpan.FromSeconds(600)
            };

            try
            {
                Console.WriteLine($"Read Start        : {DateTime.Now.ToString()}");
                Console.WriteLine("Waiting for events.  CTRL+C to exit");
                //
                // If this is IoT Hub Eventhub Compatible Endpoint, device id is in ev.Data.SystemProperties["iothub-connection-device-id"]
                //
                await foreach (PartitionEvent ev in consumer.ReadEventsAsync(startReadingAtEarliestEvent: allEvents.HasValue(), readOptions, cancellationSource.Token))
                {
                    Console.WriteLine("Enqueue at {0:yyyy/MM/dd H:mm:ss:fff} | Seq # {1:D4} | Partition {2} | Offset : {3:D6} | {4}",
                        ev.Data.EnqueuedTime,
                        ev.Data.SequenceNumber,
                        ev.Partition.PartitionId,
                        ev.Data.Offset,
                        Encoding.UTF8.GetString(ev.Data.Body.ToArray())
                        );
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception         : {ex.Message}");
            }
            Console.WriteLine($"Read Exit         : {DateTime.Now.ToString()}");

            return 1;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Please specify Event Hub Connection string");
            Console.WriteLine("dotnet run consumer -cs \"<Connection String>\" -cg <Consumer Group> -hub <Event Hub Name>");
            Console.WriteLine("  -all to read all events.  If not specified, read new events only");
        }
    }
}
