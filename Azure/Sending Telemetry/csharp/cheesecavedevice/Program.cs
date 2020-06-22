﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace simulated_device
{
    class SimulatedDevice
    {
       // Global constants.
        const float ambientTemperature = 70;                    // Ambient temperature of a southern cave, in degrees F.
        const double ambientHumidity = 99;                      // Ambient humidity in relative percentage of air saturation.
        const double desiredTempLimit = 5;                      // Acceptable range above or below the desired temp, in degrees F.
        const double desiredHumidityLimit = 10;                 // Acceptable range above or below the desired humidity, in percentages.
        const int intervalInMilliseconds = 5000;                // Interval at which telemetry is sent to the cloud.

        // Global variables.
        private static DeviceClient s_deviceClient;
        private static stateEnum fanState = stateEnum.off;                      // Initial setting of the fan.
        private static double desiredTemperature = ambientTemperature - 10;     // Initial desired temperature, in degrees F.
        private static double desiredHumidity = ambientHumidity - 20;           // Initial desired humidity in relative percentage of air saturation.

        // Enum for the state of the fan for cooling/heating, and humidifying/de-humidifying.
        enum stateEnum
        {
            off,
            on,
            failed
        }

        // The device connection string to authenticate the device with your IoT hub.
        private readonly static string s_deviceConnectionString = "HostName=CheeseCaveHub-niranjan.azure-devices.net;DeviceId=CheeseCaveID;SharedAccessKey=3LB2S3fvI+vItFkpED5NUi4F7AUNtyTLxKROBHyjBn8=";

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

        // Async method to send simulated telemetry.
        private static async void SendDeviceToCloudMessagesAsync()
        {
            double currentTemperature = ambientTemperature;         // Initial setting of temperature.
            double currentHumidity = ambientHumidity;               // Initial setting of humidity.

            Random rand = new Random();

            while (true)
            {
                // Simulate telemetry.
                double deltaTemperature = Math.Sign(desiredTemperature - currentTemperature);
                double deltaHumidity = Math.Sign(desiredHumidity - currentHumidity);

                if (fanState == stateEnum.on)
                {
                    // If the fan is on the temperature and humidity will be nudged towards the desired values most of the time.
                    currentTemperature += (deltaTemperature * rand.NextDouble()) + rand.NextDouble() - 0.5;
                    currentHumidity += (deltaHumidity * rand.NextDouble()) + rand.NextDouble() - 0.5;

                    // Randomly fail the fan.
                    if (rand.NextDouble() < 0.01)
                    {
                        fanState = stateEnum.failed;
                        redMessage("Fan has failed");
                    }
                }
                else
                {
                    // If the fan is off, or has failed, the temperature and humidity will creep up until they reaches ambient values, thereafter fluctuate randomly.
                    if (currentTemperature < ambientTemperature - 1)
                    {
                        currentTemperature += rand.NextDouble() / 10;
                    }
                    else
                    {
                        currentTemperature += rand.NextDouble() - 0.5;
                    }
                    if (currentHumidity < ambientHumidity - 1)
                    {
                        currentHumidity += rand.NextDouble() / 10;
                    }
                    else
                    {
                        currentHumidity += rand.NextDouble() - 0.5;
                    }
                }

                // Check: humidity can never exceed 100%.
                currentHumidity = Math.Min(100, currentHumidity);

                // Create JSON message.
                var telemetryDataPoint = new
                {
                    temperature = Math.Round(currentTemperature, 2),
                    humidity = Math.Round(currentHumidity, 2)
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Add custom application properties to the message.
                message.Properties.Add("sensorID", "S1");
                message.Properties.Add("fanAlert", (fanState == stateEnum.failed) ? "true" : "false");

                // Send temperature or humidity alerts, only if they occur.
                if ((currentTemperature > desiredTemperature + desiredTempLimit) || (currentTemperature < desiredTemperature - desiredTempLimit))
                {
                    message.Properties.Add("temperatureAlert", "true");
                }
                if ((currentHumidity > desiredHumidity + desiredHumidityLimit) || (currentHumidity < desiredHumidity - desiredHumidityLimit))
                {
                    message.Properties.Add("humidityAlert", "true");
                }

                Console.WriteLine("Message data: {0}", messageString);

                // Send the telemetry message.
                await s_deviceClient.SendEventAsync(message);
                greenMessage("Message sent\n");

                await Task.Delay(intervalInMilliseconds);
            }
        }
        private static void Main(string[] args)
        {
            colorMessage("Cheese Cave device app.\n", ConsoleColor.Yellow);

            // Connect to the IoT hub using the MQTT protocol.
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString, TransportType.Mqtt);

            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }
    }
}