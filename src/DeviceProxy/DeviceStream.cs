using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using IoTHubDeviceStreamSample.Shared;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;

namespace IoTHubDeviceStreamSample.DeviceProxy
{
    internal class DeviceStream
    {
        private readonly DeviceClient _deviceClient;
        private readonly string _sshDaemonHost;
        private readonly int _sshDaemonPort;
        private readonly ILogger _logger;
        private readonly DeviceStreamClientFactory _deviceStreamClientFactory;

        public DeviceStream(DeviceClient deviceClient, string sshDaemonHost, int sshDaemonPort, ILogger logger)
        {
            _deviceClient = deviceClient;
            _sshDaemonHost = sshDaemonHost;
            _sshDaemonPort = sshDaemonPort;
            _logger = logger;
            _deviceStreamClientFactory = new DeviceStreamClientFactory(logger);
        }

        public async Task RunAsync(CancellationTokenSource cancellationTokenSource)
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                _logger.LogInformation("Waiting for an incoming streaming request.");

                try
                {
                    await RunAsync(true, cancellationTokenSource).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured during streaming session.");
                }
            }
        }

        private async Task RunAsync(bool acceptDeviceStreamingRequest, CancellationTokenSource cancellationTokenSource)
        {
            var streamRequest = await _deviceClient.WaitForDeviceStreamRequestAsync(cancellationTokenSource.Token).ConfigureAwait(false);

            if (streamRequest == null)
            {
                return;
            }

            if (acceptDeviceStreamingRequest)
            {
                await _deviceClient.AcceptDeviceStreamRequestAsync(streamRequest, cancellationTokenSource.Token).ConfigureAwait(false);

                using (var webSocket = await _deviceStreamClientFactory.CreateAsync(streamRequest.Url, streamRequest.AuthorizationToken, cancellationTokenSource.Token).ConfigureAwait(false))
                {
                    using (var tcpClient = new TcpClient())
                    {
                        await tcpClient.ConnectAsync(_sshDaemonHost, _sshDaemonPort).ConfigureAwait(false);

                        using (var localStream = tcpClient.GetStream())
                        {
                            _logger.LogInformation("Starting streaming...");

                            await Task.WhenAny(
                                HandleIncomingDataAsync(localStream, webSocket, cancellationTokenSource.Token),
                                HandleOutgoingDataAsync(localStream, webSocket, cancellationTokenSource.Token)).ConfigureAwait(false);

                            localStream.Close();
                        }
                    }

                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            else
            {
                await _deviceClient.RejectDeviceStreamRequestAsync(streamRequest, cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        private static async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            var buffer = new byte[10240];

            while (remoteStream.State == WebSocketState.Open)
            {
                var receiveResult = await remoteStream.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                await localStream.WriteAsync(buffer, 0, receiveResult.Count).ConfigureAwait(false);
            }
        }

        private static async Task HandleOutgoingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            var buffer = new byte[10240];

            while (localStream.CanRead)
            {
                int receiveCount = await localStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                await remoteStream.SendAsync(
                    new ArraySegment<byte>(buffer, 0, receiveCount), 
                    WebSocketMessageType.Binary,
                    endOfMessage: true, 
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
