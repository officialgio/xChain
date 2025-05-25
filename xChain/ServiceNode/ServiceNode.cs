using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RelayService;
using xChain.Common;

namespace ServiceNode;

/// <summary>
/// Represents a Service Node in the Astra Network.
///
/// Relay Service Registration:
/// The Service Node sends its metadata (ID, URL, stake, throughput) to the Relay Service during startup.
///     If the registration fails, the Service Node logs the error.
///
/// Health Check Endpoint:
/// A /health endpoint is exposed by the Service Node.
///     The Relay Service can use this endpoint to periodically check if the node is online.
///
/// Performance Metrics Reporting:
/// The Service Node calculates its throughput (messages per second) using the PerformanceTracker class.
///     This metric is included in the registration payload sent to the Relay Service.
/// 
/// </summary>
public class ServiceNode
{
    private readonly string _nodeId;
    private readonly string _relayServiceUrl;
    private readonly HashRing<ServiceNode> _hashRing;
    private readonly ConcurrentDictionary<string, WebSocket> _connectedNodes;
    private readonly StakingManager _stakingManager;
    private readonly PerformanceTracker _performanceTracker;
    private readonly HttpClient _httpClient;

    public ServiceNode(string nodeId, string relayServiceUrl)
    {
        _nodeId = nodeId;
        _relayServiceUrl = relayServiceUrl;
        _hashRing = new HashRing<ServiceNode>();
        _connectedNodes = new ConcurrentDictionary<string, WebSocket>();
        _stakingManager = new StakingManager();
        _performanceTracker = new PerformanceTracker();
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Starts the Service Node WebSocket server and health endpoint.
    /// </summary>
    public async Task StartAsync(string url)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(url + "/");
        listener.Start();
        Console.WriteLine($"Service Node {_nodeId} started at {url}");

        // Register with the Relay Service
        await RegisterWithRelayService();

        // Start health endpoint in a separate task
        _ = Task.Run(() => StartHealthEndpoint(url));

        while (true)
        {
            var context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                HandleConnection(webSocketContext.WebSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    /// <summary>
    /// Handles a new WebSocket connection.
    /// </summary>
    private async void HandleConnection(WebSocket webSocket)
    {
        Console.WriteLine("New connection established.");

        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received: {message}");

                    // Process the message
                    var response = await ProcessMessage(message);
                    var responseBytes = Encoding.UTF8.GetBytes(response);

                    await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Connection closed.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling connection: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Processes an incoming message.
    /// </summary>
    private async Task<string> ProcessMessage(string message)
    {
        // Simulate message routing using Rendezvous Hashing
        var targetNode = _hashRing.GetNode(message);
        Console.WriteLine($"Routing message to node: {targetNode}");

        // Simulate encryption and decryption
        var (key, iv) = EncryptionHelper.GenerateAesKey();
        var encryptedMessage = EncryptionHelper.EncryptMessage(message, key, iv);
        var decryptedMessage = EncryptionHelper.DecryptMessage(encryptedMessage, key, iv);

        // Update performance metrics
        _performanceTracker.TrackMessageProcessed();

        return $"Processed message: {decryptedMessage}";
    }

    /// <summary>
    /// Registers the Service Node with the Relay Service.
    /// </summary>
    private async Task RegisterWithRelayService()
    {
        Console.WriteLine($"Registering node {_nodeId} with Relay Service at {_relayServiceUrl}...");

        var nodeInfo = new ServiceNodeInfo
        {
            NodeId = _nodeId,
            Url = "http://localhost:5001",
            Stake = _stakingManager.GetStake(_nodeId),
            Throughput = _performanceTracker.GetThroughput()
        };

        var content = new StringContent(JsonSerializer.Serialize(nodeInfo), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_relayServiceUrl + "/register", content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Node {_nodeId} registered successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to register node {_nodeId}. Status code: {response.StatusCode}");
        }
    }

    /// <summary>
    /// Starts the health endpoint for the Service Node.
    /// </summary>
    private async Task StartHealthEndpoint(string url)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(url + "/health/");
        listener.Start();
        Console.WriteLine("Health endpoint started.");

        while (true)
        {
            var context = await listener.GetContextAsync();
            var response = context.Response;

            try
            {
                response.StatusCode = 200;
                var status = new { Status = "Healthy" };
                var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(status));
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health endpoint error: {ex.Message}");
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
        }
    }
}