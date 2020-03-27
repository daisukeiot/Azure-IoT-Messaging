
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.CommandLineUtils;

namespace Procucer
{
    class Producer
    {
        private const int numEvents = 12;
        private const string characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        static async Task<int> Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.FullName = "Event Hubs Producer";
            app.Description = "Event Hubs Consumer Sample";
            app.HelpOption("-h|-?|--help");

            var connectionString = app.Option("-cs", "Specify Connection String", CommandOptionType.SingleValue);
            var hub              = app.Option("-hub", "Specify Consumer Group", CommandOptionType.SingleValue);

            app.Execute(args);

            if (!connectionString.HasValue() || string.IsNullOrEmpty(connectionString.Value()))
            {
                ShowHelp();
                return 0;
            }

            if (!hub.HasValue() || string.IsNullOrEmpty(hub.Value()))
            {
                ShowHelp();
                return 0;
            }

            Console.WriteLine("Connection String : {0}....", connectionString.Value().Replace("\"", "").Trim().Substring(0,50));
            Console.WriteLine($"Event Hub         : {hub.Value().Replace("\"", "").Trim()}");

            //
            // https://docs.microsoft.com/en-us/dotnet/api/azure.messaging.eventhubs.producer.eventhubproducerclient?view=azure-dotnet
            //
            await using (EventHubProducerClient ehProducer = new EventHubProducerClient(connectionString.Value().Replace("\"", "").Trim(), hub.Value().Replace("\"", "").Trim()))
            {
                try
                {
                    var eventHubProperty = await ehProducer.GetEventHubPropertiesAsync();

                    Console.WriteLine("--------------------------------------------------------");
                    Console.WriteLine($"Sending {numEvents} individual events");


                    for (var i = 0; i < numEvents; i++)
                    {
                        using (EventDataBatch eventData = await ehProducer.CreateBatchAsync())
                        {
                            var message = string.Format("Message # {0:D2}", i);
                            eventData.TryAdd(new EventData(Encoding.UTF8.GetBytes(message)));
                            await ehProducer.SendAsync(eventData);
                        }
                    }

                    Console.WriteLine("--------------------------------------------------------");
                    Console.WriteLine($"Sending {numEvents} events in a batch");
                    Console.WriteLine($"Press Enter to continue, or CTRL+C to exit");
                    char ch = Console.ReadKey(true).KeyChar;

                    for (var i = 0; i < eventHubProperty.PartitionIds.Length; i++)
                    {
                        using (EventDataBatch eventData = await ehProducer.CreateBatchAsync())
                        {
                            for (var j = 0; j < numEvents; j++)
                            {
                                var message = string.Format("Message # {0:D2} Batch {1:D2}", j, i);
                                eventData.TryAdd(new EventData(Encoding.UTF8.GetBytes(message)));
                            }
                            await ehProducer.SendAsync(eventData);
                        }
                    }


                    Console.WriteLine("--------------------------------------------------------");
                    Console.WriteLine($"Sending {numEvents} individual events with Partition Key");
                    Console.WriteLine($"Press Enter to continue, or CTRL+C to exit");
                    ch = Console.ReadKey(true).KeyChar;

                    var random = new Random();
                    var keyList = new List<String>();

                    for (var i = 0; i < eventHubProperty.PartitionIds.Length; i++)
                    {
                        // Generate 6 letter random string
                        StringBuilder result = new StringBuilder(6);
                        for (int j = 0; j < 6; j++) {
                            result.Append(characters[random.Next(characters.Length)]);
                        }

                        keyList.Add(string.Format("{0,6:N0}", result.ToString()));
                    }

                    for (var i = 0; i < numEvents * eventHubProperty.PartitionIds.Length; i++)
                    {
                        var rand = random.Next(eventHubProperty.PartitionIds.Length);

                        CreateBatchOptions batchOption = new CreateBatchOptions {PartitionKey = keyList[rand] };

                        using (EventDataBatch eventData = await ehProducer.CreateBatchAsync(batchOption))
                        {
                            var message = string.Format("Message # {0:D2} with Key {1}", i, batchOption.PartitionKey);
                            eventData.TryAdd(new EventData(Encoding.UTF8.GetBytes(message)));
                            await ehProducer.SendAsync(eventData);
                        }
                    }

                    Console.WriteLine("--------------------------------------------------------");
                    var partitionIds = await ehProducer.GetPartitionIdsAsync();
                    Console.WriteLine($"Sending {partitionIds.Length} events to each partition");
                    Console.WriteLine($"Press Enter to continue, or CTRL+C to exit");
                    ch = Console.ReadKey(true).KeyChar;

                    foreach(var partitionId in partitionIds)
                    {
                        CreateBatchOptions batchOption = new CreateBatchOptions { PartitionId = partitionId };
                        for (var i = 0; i < numEvents / eventHubProperty.PartitionIds.Length; i++)
                        {
                            using (EventDataBatch eventData = await ehProducer.CreateBatchAsync(batchOption))
                            {
                                var message = string.Format("Message # {0:D2} to Partition {1:D2}", i, partitionId);
                                eventData.TryAdd(new EventData(Encoding.UTF8.GetBytes(message)));
                                await ehProducer.SendAsync(eventData);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{Environment.NewLine}Exception: {ex.Message}");
                }
                await ehProducer.CloseAsync();
            }

            return 1;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Please specify Event Hub Connection string");
            Console.WriteLine("dotnet run producer -cs \"<Connection String>\" -hub <Event Hub Name>");
        }
    }
}
