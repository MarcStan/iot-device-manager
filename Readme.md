# IoT device manager

This is a **demonstration UI** for multi-device management when using [IoT Hub](https://azure.microsoft.com/services/iot-hub/). As such it only supports limited scenarios.

If you need more features, consider using [Azure IoT Explorer](https://github.com/Azure/azure-iot-explorer) (or the deprecated [DeviceExplorer](https://github.com/Azure/azure-iot-sdk-csharp/tree/master/tools/DeviceExplorer)).

# Setup instructions

All commands use the [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli). It is preinstalled in the [Azure Cloud shell](https://azure.microsoft.com/features/cloud-shell).

## Azure

You need to create an IoT hub instance. You must either use the free or S tier (Basic tier will not work as it lacks cloud to device capabilities!). 

Once the IoT hub exists run the cloudshell and execute (pick a name for {myDeviceName} such as `MyIoTDemoClient`):

``` powershell
az extension add --name azure-cli-iot-ext

az iot hub device-identity create --hub-name {YourIoTHubName} --device-id {myDeviceName}

az iot hub device-identity show-connection-string --hub-name {YourIoTHubName} --device-id {myDeviceName} --output table
```

## Local

The demo client requires the connection string from above, so paste it into the appsettings.json (or use user secrets manager as described in the json).

With the connection string the sample client should now be able to run.

It will send random telemetry data to the IoT hub every second while it's running.
