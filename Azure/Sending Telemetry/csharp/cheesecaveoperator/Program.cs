﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Azure.EventHubs;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace cheesecave_operator
{
    class ReadDeviceToCloudMessages
    {
        // Global variables.
        // The Event Hub-compatible endpoint.
        private readonly static string s_eventHubsCompatibleEndpoint = "sb://iothub-ns-cheesecave-3661157-88be6c3d49.servicebus.windows.net/";

        // The Event Hub-compatible name.
        private readonly static string s_eventHubsCompatiblePath = "cheesecavehub-niranjan";
        private readonly static string s_iotHubSasKey = "WoVCU9ilw65kcT139QPwe5xSyx9Rj7DQtSQhu0YVmQY=";
        private readonly static string s_iotHubSasKeyName = "service";
        private static EventHubClient s_eventHubClient;

        // Connection string for your IoT Hub.
        private readonly static string s_serviceConnectionString = "<your service connection string>";

        // Asynchronously create a PartitionReceiver for a partition and then start reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(string partition)
        {
            // Create the receiver using the default consumer group.
            var eventHubReceiver = s_eventHubClient.CreateReceiver("$Default", partition, EventPosition.FromEnqueuedTime(DateTime.Now));
            Console.WriteLine("Created receiver on partition: " + partition);

            while (true)
            {
                // Check for EventData - this methods times out if there is nothing to retrieve.
                var events = await eventHubReceiver.ReceiveAsync(100);

                // If there is data in the batch, process it.
                if (events == null) continue;

                foreach (EventData eventData in events)
                {
                    string data = Encoding.UTF8.GetString(eventData.Body.Array);

                    greenMessage("Telemetry received: " + data);

                    foreach (var prop in eventData.Properties)
                    {
                        if (prop.Value.ToString() == "true")
                        {
                            redMessage(prop.Key);
                        }
                    }
                    Console.WriteLine();
                }
            }
        }

        public static void Main(string[] args)
        {
            colorMessage("Cheese Cave Operator\n", ConsoleColor.Yellow);

            // Create an EventHubClient instance to connect to the IoT Hub Event Hubs-compatible endpoint.
            var connectionString = new EventHubsConnectionStringBuilder(new Uri(s_eventHubsCompatibleEndpoint), s_eventHubsCompatiblePath, s_iotHubSasKeyName, s_iotHubSasKey);
            s_eventHubClient = EventHubClient.CreateFromConnectionString(connectionString.ToString());

            // Create a PartitionReceiver for each partition on the hub.
            var runtimeInfo = s_eventHubClient.GetRuntimeInformationAsync().GetAwaiter().GetResult();
            var d2cPartitions = runtimeInfo.PartitionIds;

            // Create receivers to listen for messages.
            var tasks = new List<Task>();
            foreach (string partition in d2cPartitions)
            {
                tasks.Add(ReceiveMessagesFromDeviceAsync(partition));
            }

            // Wait for all the PartitionReceivers to finish.
            Task.WaitAll(tasks.ToArray());
        }

        private static void colorMessage(string text, ConsoleColor clr)
        {
            Console.ForegroundColor = clr;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        private static void greenMessage(string text)
        {
            colorMessage(text, ConsoleColor.Green);
        }

        private static void redMessage(string text)
        {
            colorMessage(text, ConsoleColor.Red);
        }
    }
}