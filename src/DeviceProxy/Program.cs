using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;

namespace IoTHubDeviceStreamSample.DeviceProxy
{
    public static class Program
    {
        public static async Task<int> Main()
        {
            var logger = CreateLogger();

            var deviceConnectionString = GetEnvironmentVariableValue("IOTHUB_DEVICE_CONNECTION_STRING", string.Empty);
            var hostName = GetEnvironmentVariableValue("SSH_DAEMON_HOSTNAME", "localhost");
            var port = GetEnvironmentVariableValue("SSH_DAEMON_PORT", 22);

            if (string.IsNullOrWhiteSpace(deviceConnectionString))
            {
                logger.LogCritical("Please provide a connection string, target host and port.");
                return 1;
            }

            using (var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Amqp_WebSocket_Only))
            {
                if (deviceClient == null)
                {
                    logger.LogCritical("Failed to create device client used for IoT Hub communication.");
                    return 1;
                }

                ShowApplicationInformation(deviceConnectionString, hostName, port);

                var streamingProxy = new DeviceStream(deviceClient, hostName, port, logger);
                await streamingProxy.RunAsync(new CancellationTokenSource()).ConfigureAwait(false);
            }

            logger.LogInformation("Shutdown completed.");
            return 0;
        }

        private static void ShowApplicationInformation(string deviceConnectionString, string hostName, int port)
        {
            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(deviceConnectionString);

            Console.WriteLine("Microsoft Azure IoT Hub - DeviceStreams");
            Console.WriteLine("Example: How to establish a SSH connection to IoT devices");
            Console.WriteLine(">>> DeviceProxy <<<");
            Console.WriteLine("\n-------------------------------------------------------\n");

            Console.WriteLine("IoT Hub-Configuration");
            Console.WriteLine($" > IoT Hub: {connectionStringBuilder.HostName}");
            Console.WriteLine($" > Device identifier: {connectionStringBuilder.DeviceId}\n");

            Console.WriteLine("SSH-Configuration");
            Console.WriteLine($" > Host: {hostName}");
            Console.WriteLine($" > Port: {port}");

            Console.WriteLine("\n-------------------------------------------------------\n");
        }

        private static ILogger CreateLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("DeviceProxy", LogLevel.Trace)
                    .AddConsole();
            });

            return loggerFactory.CreateLogger("DeviceProxy");
        }

        private static T GetEnvironmentVariableValue<T>(string key, T defaultValue)
        {
            try
            {
                var value = Environment.GetEnvironmentVariable(key);

                if (string.IsNullOrWhiteSpace(value))
                {
                    return defaultValue;
                }

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
