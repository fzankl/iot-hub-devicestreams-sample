# Azure IoT Hub - DeviceStreams Sample
**Example how to work with Azure IoT Hub Device Streams**

-------------------------------------

This example shows how to work with Azure IoT Hub - Device Streams when using SSH to connect to a IoT device via Internet/IoT Hub.

`DeviceProxy` represents local proxy for IoT device which acts as client for the local SSH daemon. It's responsible for authenticating against IoT Hub as well as creating a WebSocket connection to streaming endpoint of Azure IoT Hub.

`ServiceProxy` represents proxy for service which acts as a server for a local SSH client. It's responsible for authenticating against IoT Hub as well as creating a WebSocket connection to streaming endpoint of Azure IoT Hub.

`DeviceProxy` and `ServiceProxy` communicates with each other using created WebSocket via streaming endpoint of Azure IoT Hub.

Further information: https://docs.microsoft.com/de-de/azure/iot-hub/iot-hub-device-streams-overview