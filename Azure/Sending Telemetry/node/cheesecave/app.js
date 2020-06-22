// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

'use strict';
const chalk = require('chalk');
console.log(chalk.yellow('Cheese cave device app'));

// The device connection string to authenticate the device with your IoT hub.
const connectionString = 'HostName=CheeseCaveHub-niranjan.azure-devices.net;DeviceId=CheeseCaveID;SharedAccessKey=3LB2S3fvI+vItFkpED5NUi4F7AUNtyTLxKROBHyjBn8=';

// The sample connects to a device-specific MQTT endpoint on your IoT Hub.
const Mqtt = require('azure-iot-device-mqtt').Mqtt;
const DeviceClient = require('azure-iot-device').Client
const Message = require('azure-iot-device').Message;

const client = DeviceClient.fromConnectionString(connectionString, Mqtt);

// Global variables.
const ambientTemperature = 70;                          // Ambient temperature of a southern cave, in degrees F.
const ambientHumidity = 99;                             // Ambient humidity in relative percentage of air saturation.
let desiredTemperature = ambientTemperature-10;         // Initial desired temperature, in degrees F.
const desiredTempLimit = 5;                             // Acceptable range above or below the desired temp, in degrees F.
let desiredHumidity = ambientHumidity - 20;             // Initial desired humidity in relative percentage of air saturation.
const desiredHumidityLimit = 10;                        // Acceptable range above or below the desired humidity, in percentages.
const intervalInMilliseconds = 5000;                    // Interval at which telemetry is sent to the cloud.

// Enum for the state of the fan for cooling/heating, and humidifying/de-humidifying.
const stateEnum = Object.freeze({ "off": "off", "on": "on", "failed": "failed" });
let fanState = stateEnum.off;

let currentTemperature = ambientTemperature;            // Initial setting of temperature.
let currentHumidity = ambientHumidity;                  // Initial setting of humidity.

function greenMessage(text) {
    console.log(chalk.green(text));
}

function redMessage(text) {
    console.log(chalk.red(text));
}

// Send telemetry messages to your hub.
function sendMessage() {

    let deltaTemperature = Math.sign(desiredTemperature - currentTemperature);
    let deltaHumidity = Math.sign(desiredHumidity - currentHumidity);

    if (fanState == stateEnum.on) {

        // If the fan is on the temperature and humidity will be nudged towards the desired values most of the time.
        currentTemperature += (deltaTemperature * Math.random()) + Math.random() - 0.5;
        currentHumidity += (deltaHumidity * Math.random()) + Math.random() - 0.5;

        // Randomly fail the fan.
        if (Math.random() < 0.01) {
            fanState = stateEnum.failed;
            redMessage('Fan has failed');
        }
    }
    else {
        // If the fan is off, or has failed, the temperature and humidity will creep up until they reach ambient values, thereafter fluctuate randomly.
        if (currentTemperature < ambientTemperature - 1) {
            currentTemperature += Math.random() / 10;
        } else {
            currentTemperature += Math.random() - 0.5;
        }
        if (currentHumidity < ambientHumidity - 1) {
            currentHumidity += Math.random() / 10;
        } else {
            currentHumidity += Math.random() - 0.5;
        }
    }

    // Check: humidity can never exceed 100%.
    currentHumidity = Math.min(100, currentHumidity);

    // Prepare the telemetry message.
    const message = new Message(JSON.stringify({
        temperature: currentTemperature.toFixed(2),
        humidity: currentHumidity.toFixed(2),
    }));

    // Add custom application properties to the message.
    // An IoT hub can filter on these properties without access to the message body.
    message.properties.add('sensorID', "S1");
    message.properties.add('fanAlert', (fanState == stateEnum.failed) ? 'true' : 'false');

    // Send temperature or humidity alerts, only if they occur.
    if ((currentTemperature > desiredTemperature + desiredTempLimit) || (currentTemperature < desiredTemperature - desiredTempLimit)) {
        message.properties.add('temperatureAlert', 'true');
    }
    if ((currentHumidity > desiredHumidity + desiredHumidityLimit) || (currentHumidity < desiredHumidity - desiredHumidityLimit)) {
        message.properties.add('humidityAlert', 'true');
    }

    console.log('\nMessage data: ' + message.getData());

    // Send the telemetry message.
    client.sendEvent(message, function (err) {
        if (err) {
            redMessage('Send error: ' + err.toString());
        } else {
            greenMessage('Message sent');
        }
    });
}

// Set up the telemetry interval.
setInterval(sendMessage, intervalInMilliseconds);