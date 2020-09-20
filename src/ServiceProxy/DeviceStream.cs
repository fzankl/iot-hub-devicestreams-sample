using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using IoTHubDeviceStreamSample.Shared;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Logging;

namespace IoTHubDeviceStreamSample.ServiceProxy
{
    internal class DeviceStream
    {
        private readonly ServiceClient _serviceClient;
        private readonly string _deviceId;
        private readonly int _localPort;
        private readonly ILogger _logger;
        private readonly DeviceStreamClientFactory _deviceStreamClientFactory;

        private TcpListener _tcpListener;

        public DeviceStream(ServiceClient deviceClient, string deviceId, int localPort, ILogger logger)
        {
            _serviceClient = deviceClient;
            _deviceId = deviceId;
            _localPort = localPort;
            _logger = logger;
            _deviceStreamClientFactory = new DeviceStreamClientFactory(logger);
        }

        public async Task RunAsync(CancellationTokenSource cancellationTokenSource)
        {
            _tcpListener = new TcpListener(IPAddress.Loopback, _localPort);
            _tcpListener.Start();

            _logger.LogInformation("Waiting for TCP clients...");

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);

                _logger.LogInformation("Accepted TCP client using endpoint '{0}'.", tcpClient.Client.LocalEndPoint.ToString() ?? string.Empty);

                await HandleIncomingConnectionsAndCreateStreams(_deviceId, _serviceClient, tcpClient, cancellationTokenSource).ConfigureAwait(false);
            }
        }

        private async Task HandleIncomingConnectionsAndCreateStreams(string deviceId, ServiceClient serviceClient, TcpClient tcpClient, CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                var deviceStreamRequest = new DeviceStreamRequest("ServiceStream");

                using (var localStream = tcpClient.GetStream())
                {
                    var result = await serviceClient.CreateStreamAsync(deviceId, deviceStreamRequest, cancellationTokenSource.Token).ConfigureAwait(false);

                    _logger.LogInformation("Stream response received: Name={0} IsAccepted={1}.", result.StreamName, result.IsAccepted);

                    if (result.IsAccepted)
                    {
                        try
                        {
                            using (var remoteStream = await _deviceStreamClientFactory.CreateAsync(result.Uri, result.AuthorizationToken, cancellationTokenSource.Token).ConfigureAwait(false))
                            {
                                _logger.LogInformation("Streaming started for stream: {0}.", result.StreamName);

                                await Task.WhenAny(
                                    HandleIncomingDataAsync(localStream, remoteStream, cancellationTokenSource.Token),
                                    HandleOutgoingDataAsync(localStream, remoteStream, cancellationTokenSource.Token)).ConfigureAwait(false);
                            }

                            _logger.LogInformation("Finished streaming for stream: {0}.", result.StreamName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "An error occured during creation of streaming session.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during streaming session.");
            }
            finally
            {
                tcpClient.Close();
            }
        }

        private static async Task HandleIncomingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            var receiveBuffer = new byte[10240];

            while (localStream.CanRead)
            {
                var receiveResult = await remoteStream.ReceiveAsync(receiveBuffer, cancellationToken).ConfigureAwait(false);
                await localStream.WriteAsync(receiveBuffer, 0, receiveResult.Count).ConfigureAwait(false);
            }
        }

        private static async Task HandleOutgoingDataAsync(NetworkStream localStream, ClientWebSocket remoteStream, CancellationToken cancellationToken)
        {
            var buffer = new byte[10240];

            while (remoteStream.State == WebSocketState.Open)
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
