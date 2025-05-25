using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RelayService;
using xChain.Common;

namespace GatewayNode;

/// <summary>
/// Represents a Gateway Node in the Astra Network.
/// </summary>
public class GatewayNode
{
    private readonly string _relayServiceUrl;
    private readonly HashRing<ServiceNodeInfo> _hashRing;
    private readonly ConcurrentDictionary<string, WebSocket> _connectedClients;
    private readonly HttpClient _httpClient;

    public GatewayNode(string relayServiceUrl)
    {
        _relayServiceUrl = relayServiceUrl;
        _hashRing = new HashRing<ServiceNodeInfo>();
        _connectedClients = new ConcurrentDictionary<string, WebSocket>();
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Starts the Gateway Node WebSocket server.
    /// </summary>
    public async Task StartAsync(string url)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(url + "/");
        listener.Start();
        Console.WriteLine($"Gateway Node started at {url}");

        // Fetch the list of Service Nodes from the Relay Service
        await FetchServiceNodesAsync();

        // Periodically update the list of Service Nodes
        _ = Task.Run(() => MonitorServiceNodesAsync());

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
    /// Handles a new WebSocket connection from a wallet or dApp.
    /// </summary>
    private async void HandleConnection(WebSocket webSocket)
    {
        Console.WriteLine("New client connected.");

        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message from client: {message}");

                    // Route the message to the appropriate Service Node
                    var response = await RouteMessageToServiceNode(message);
                    var responseBytes = Encoding.UTF8.GetBytes(response);

                    await webSocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Client disconnected.");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client connection: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Routes a message to the appropriate Service Node.
    /// </summary>
    private async Task<string> RouteMessageToServiceNode(string message)
    {
        if (_hashRing == null || _hashRing.GetNode(message) == null)
        {
            return "No Service Nodes available to handle the request.";
        }

        var targetNode = _hashRing.GetNode(message);
        Console.WriteLine($"Routing message to Service Node: {targetNode.NodeId}");

        try
        {
            // Forward the message to the target Service Node
            var content = new StringContent(message, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(targetNode.Url + "/process", content);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return $"Failed to route message to Service Node {targetNode.NodeId}. Status code: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error routing message to Service Node: {ex.Message}");
            return $"Error routing message to Service Node {targetNode.NodeId}.";
        }
    }

    /// <summary>
    /// Fetches the list of active Service Nodes from the Relay Service.
    /// </summary>
    private async Task FetchServiceNodesAsync()
    {
        try
        {
            Console.WriteLine("Fetching Service Nodes from Relay Service...");
            var response = await _httpClient.GetAsync(_relayServiceUrl + "/nodes");
            if (response.IsSuccessStatusCode)
            {
                var nodes = JsonSerializer.Deserialize<ServiceNodeInfo[]>(await response.Content.ReadAsStringAsync());
                if (nodes != null)
                {
                    foreach (var node in nodes)
                    {
                        _hashRing.AddNode(node);
                    }
                    Console.WriteLine($"Loaded {_hashRing.GetNodeCount()} Service Nodes.");
                }
            }
            else
            {
                Console.WriteLine($"Failed to fetch Service Nodes. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Service Nodes: {ex.Message}");
        }
    }

    /// <summary>
    /// Periodically updates the list of Service Nodes from the Relay Service.
    /// </summary>
    private async Task MonitorServiceNodesAsync()
    {
        while (true)
        {
            await FetchServiceNodesAsync();
            await Task.Delay(10000); // Update every 10 seconds
        }
    }
}