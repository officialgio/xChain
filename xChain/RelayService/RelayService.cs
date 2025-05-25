using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace RelayService;

/// <summary>
/// Represents the Relay Service for the Astra Network.
/// </summary>
public class RelayService
{
    private readonly ConcurrentDictionary<string, ServiceNodeInfo> _serviceNodes;
    private readonly HttpClient _httpClient;

    public RelayService()
    {
        _serviceNodes = new ConcurrentDictionary<string, ServiceNodeInfo>();
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Starts the Relay Service HTTP server.
    /// </summary>
    public async Task StartAsync(string url)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(url + "/");
        listener.Start();
        Console.WriteLine($"Relay Service started at {url}");

        while (true)
        {
            var context = await listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/register")
            {
                // Handle Service Node registration
                await HandleRegisterNodeAsync(request, response);
            }
            else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/nodes")
            {
                // Handle node discovery
                await HandleGetNodesAsync(response);
            }
            else
            {
                response.StatusCode = 404;
                response.Close();
            }
        }
    }

    /// <summary>
    /// Handles Service Node registration.
    /// </summary>
    private async Task HandleRegisterNodeAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            using var reader = new System.IO.StreamReader(request.InputStream);
            var body = await reader.ReadToEndAsync();
            var nodeInfo = JsonSerializer.Deserialize<ServiceNodeInfo>(body);

            if (nodeInfo != null && !_serviceNodes.ContainsKey(nodeInfo.NodeId))
            {
                _serviceNodes[nodeInfo.NodeId] = nodeInfo;
                Console.WriteLine($"Registered Service Node: {nodeInfo.NodeId}");
                response.StatusCode = 200;
            }
            else
            {
                response.StatusCode = 400;
                Console.WriteLine($"Failed to register Service Node: {nodeInfo?.NodeId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering node: {ex.Message}");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }
    }

    /// <summary>
    /// Handles node discovery by returning the list of active Service Nodes.
    /// </summary>
    private async Task HandleGetNodesAsync(HttpListenerResponse response)
    {
        try
        {
            var nodes = JsonSerializer.Serialize(_serviceNodes.Values);
            var buffer = System.Text.Encoding.UTF8.GetBytes(nodes);

            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.StatusCode = 200;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching nodes: {ex.Message}");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }
    }

    /// <summary>
    /// Periodically pings Service Nodes to ensure they are online.
    /// </summary>
    public async Task MonitorNodesAsync()
    {
        while (true)
        {
            foreach (var nodeId in _serviceNodes.Keys)
            {
                var nodeInfo = _serviceNodes[nodeId];
                try
                {
                    var response = await _httpClient.GetAsync(nodeInfo.Url + "/health");
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Node {nodeId} is offline. Removing from registry.");
                        _serviceNodes.TryRemove(nodeId, out _);
                    }
                }
                catch
                {
                    Console.WriteLine($"Node {nodeId} is offline. Removing from registry.");
                    _serviceNodes.TryRemove(nodeId, out _);
                }
            }

            await Task.Delay(5000); // Check every 5 seconds
        }
    }
}

/// <summary>
/// Represents metadata about a Service Node.
/// </summary>
public class ServiceNodeInfo
{
    public string NodeId { get; set; }
    public string Url { get; set; }
    public decimal Stake { get; set; }
    public double Throughput { get; set; }
}
