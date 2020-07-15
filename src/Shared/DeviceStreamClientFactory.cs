using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IoTHubDeviceStreamSample.Shared
{
    public class DeviceStreamClientFactory
    {
        private readonly ILogger _logger;

        public DeviceStreamClientFactory(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a WebSocket with the proper authorization header for Device Streams.
        /// </summary>
        /// <param name="url">Url to the Streaming Gateway.</param>
        /// <param name="authorizationToken">Authorization token to connect to the Streaming Gateway.</param>
        /// <param name="cancellationToken">The token used for cancelling this operation if desired.</param>
        /// <returns>A ClientWebSocket instance connected to the Device Streaming gateway, if successful.</returns>
        public async Task<ClientWebSocket> CreateAsync(Uri url, string authorizationToken, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Try to connect to '{0}'", url);
            
            var webSocketClient = new ClientWebSocket();
            webSocketClient.Options.SetRequestHeader("Authorization", $"Bearer {authorizationToken}");

            await webSocketClient.ConnectAsync(url, cancellationToken).ConfigureAwait(false);

            return webSocketClient;
        }
    }
}
