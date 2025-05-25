using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace ServiceNode;

public class WebSocketServer
{
    public async Task StartAsync(string url)
    {
        using var listener = new HttpListener();
        
        listener.Prefixes.Add(url + "/");
        listener.Start();
        Console.WriteLine($"WebSocket server started at {url}");

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
    
    private async void HandleConnection(WebSocket webSocket)
    {
        Console.WriteLine("Client connected.");
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received: {message}");

                // Echo the message back to the client
                var response = Encoding.UTF8.GetBytes("Message received: " + message);
                await webSocket.SendAsync(new ArraySegment<byte>(response), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("Client disconnected.");
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
}