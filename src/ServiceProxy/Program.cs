using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;

namespace IoTHubDeviceStreamSample.ServiceProxy
{
    public static class Program
    {
        public static async Task<int> Main()
        {
            var logger = CreateLogger();

            var deviceConnectionString = GetEnvironmentVariableValue("IOTHUB_CONNECTION_STRING", string.Empty);

            if (string.IsNullOrWhiteSpace(deviceConnectionString))
            {
                logger.LogCritical("Please provide a connection string, device identifier and local port.");
                return 1;
            }

            var serviceClient = ServiceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Amqp);

            if (serviceClient == null)
            {
                logger.LogCritical("Failed to create service client used for IoT Hub communication.");
                return 1;
            }

            var streamingProxy = new DeviceStream(serviceClient, "iot-device-sample", 2222, logger);
            await streamingProxy.RunAsync(new CancellationTokenSource()).ConfigureAwait(false);

            logger.LogInformation("Shutdown completed.");
            return 0;
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
