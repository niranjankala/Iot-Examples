// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

'use strict';

const chalk = require('chalk');
console.log(chalk.yellow('Cheese Cave Operator: the back-end service app'));

// The connection string for the IoT Hub service.
const connectionString = 'HostName=CheeseCaveHub-niranjan.azure-devices.net;DeviceId=CheeseCaveID;SharedAccessKey=3LB2S3fvI+vItFkpED5NUi4F7AUNtyTLxKROBHyjBn8=';

// The device ID.
const deviceId = 'CheeseCaveID';

// The sample connects to service-side endpoint to call direct methods on devices.
const Client = require('azure-iothub').Client;
const Registry = require('azure-iothub').Registry;

// Connect to the service-side endpoint on your IoT hub.
const client = Client.fromConnectionString(connectionString);

// The sample connects to an IoT hub's Event Hubs-compatible endpoint to read messages sent from a device.
const { EventHubClient, EventPosition } = require('@azure/event-hubs');

let eventHubClient;

function greenMessage(text) {
    console.log(chalk.green(text));
}

function redMessage(text) {
    console.log(chalk.red(text));
}

function printError(err) {
    redMessage(err.message);
};

// Display the message content - telemetry and properties.
function printMessage(message) {

    greenMessage('Telemetry received: ' + JSON.stringify(message.body));
    if (message.applicationProperties.fanAlert == 'true') {
        redMessage('Fan alert');
    }
    if (message.applicationProperties.temperatureAlert == 'true') {
        redMessage('Temperature alert');
    }
    if (message.applicationProperties.humidityAlert == 'true') {
        redMessage('Humidity alert');
    }
    console.log('');
};

// Connect to the partitions on the IoT Hub's Event Hubs-compatible endpoint.
EventHubClient.createFromIotHubConnectionString(connectionString).then(function (client) {
    greenMessage("Successfully created the EventHub Client from IoT Hub connection string.");

    // Save the client as a global variable.
    eventHubClient = client;

    return eventHubClient.getPartitionIds();

}).then(function (ids) {
    console.log("The partition ids are: ", ids);
    console.log('');
    return ids.map(function (id) {
        return eventHubClient.receive(id, printMessage, printError, { eventPosition: EventPosition.fromEnqueuedTime(Date.now()) });
    });
}).catch(printError);