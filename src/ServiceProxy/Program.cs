using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;

namespace IoTHubDeviceStreamSample.ServiceProxy
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var logger = CreateLogger();

            var serviceConnectionString = GetEnvironmentVariableValue("IOTHUB_SERVICE_CONNECTION_STRING", string.Empty);
            var deviceId = GetEnvironmentVariableValue("IOTHUB_DEVICE_IDENTIFIER", string.Empty);
            var port = GetEnvironmentVariableValue("SSH_PORT", 2222);

            if (string.IsNullOrWhiteSpace(serviceConnectionString))
            {
                logger.LogCritical("Please provide a connection string, device identifier and local port.");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                logger.LogCritical("Invalid device identifier provided.");
                return 1;
            }

            var serviceClient = ServiceClient.CreateFromConnectionString(serviceConnectionString, TransportType.Amqp);

            if (serviceClient == null)
            {
                logger.LogCritical("Failed to create service client used for IoT Hub communication.");
                return 1;
            }

            ShowApplicationInformation(serviceConnectionString, deviceId, port);

            var streamingProxy = new DeviceStream(serviceClient, deviceId, port, logger);
            await streamingProxy.RunAsync(new CancellationTokenSource()).ConfigureAwait(false);

            logger.LogInformation("Shutdown completed.");
            return 0;
        }

        private static void ShowApplicationInformation(string serviceConnectionString, string deviceId, int port)
        {
            var connectionStringBuilder = IotHubConnectionStringBuilder.Create(serviceConnectionString);

            Console.WriteLine("Microsoft Azure IoT Hub - DeviceStreams");
            Console.WriteLine("Example: How to establish a SSH connection to IoT devices");
            Console.WriteLine(">>> ServiceProxy <<<");
            Console.WriteLine("\n-------------------------------------------------------\n");

            Console.WriteLine("IoT Hub-Configuration");
            Console.WriteLine($" > IoT Hub: {connectionStringBuilder.HostName}");
            Console.WriteLine($" > SharedAccessKeyName: {connectionStringBuilder.SharedAccessKeyName}");
            Console.WriteLine($" > Device identifier: {deviceId}\n");

            Console.WriteLine("SSH-Configuration");
            Console.WriteLine(" > Host: localhost");
            Console.WriteLine($" > Port: {port}\n");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($" ssh localhost -p {port}");
            Console.ResetColor();

            Console.WriteLine("\n-------------------------------------------------------\n");
        }

        private static ILogger CreateLogger()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("ServiceProxy", LogLevel.Trace)
                    .AddConsole();
            });

            return loggerFactory.CreateLogger("ServiceProxy");
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
