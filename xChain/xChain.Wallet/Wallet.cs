using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using xChain.Common;

namespace xChain.Wallet;

/// <summary>
/// Represents a Wallet Client in the Astra Network.
///
/// WebSocket Connection:
///     Connects to the Gateway Node using a WebSocket connection.
///
/// Message Encryption:
///     Encrypts the message using AES encryption before sending it to the Gateway Node.
///
/// Send Payload:
///     Sends a JSON payload containing the encrypted message, AES key, and IV to the Gateway Node.
///     Receive and Decrypt Response:
///
/// Receives the response from the Gateway Node.
///     Decrypts the response using the AES key and IV.
/// </summary>
public class WalletClient
{
    private readonly string _gatewayNodeUrl;
    private ClientWebSocket _webSocket;

    public WalletClient(string gatewayNodeUrl)
    {
        _gatewayNodeUrl = gatewayNodeUrl;
        _webSocket = new ClientWebSocket();
    }

    /// <summary>
    /// Connects to the Gateway Node.
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            Console.WriteLine($"Connecting to Gateway Node at {_gatewayNodeUrl}...");
            await _webSocket.ConnectAsync(new Uri(_gatewayNodeUrl), CancellationToken.None);
            Console.WriteLine("Connected to Gateway Node.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to Gateway Node: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends an encrypted message to the Gateway Node.
    /// </summary>
    public async Task SendMessageAsync(string message)
    {
        try
        {
            // Encrypt the message
            var (key, iv) = EncryptionHelper.GenerateAesKey();
            var encryptedMessage = EncryptionHelper.EncryptMessage(message, key, iv);

            // Create a payload with the encrypted message and AES key/IV
            var payload = new
            {
                EncryptedMessage = Convert.ToBase64String(encryptedMessage),
                AesKey = Convert.ToBase64String(key),
                Iv = Convert.ToBase64String(iv)
            };

            var payloadJson = JsonSerializer.Serialize(payload);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            // Send the payload to the Gateway Node
            await _webSocket.SendAsync(new ArraySegment<byte>(payloadBytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine("Message sent to Gateway Node.");

            // Wait for a response
            await ReceiveResponseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send message: {ex.Message}");
        }
    }

    /// <summary>
    /// Receives and decrypts a response from the Gateway Node.
    /// </summary>
    private async Task ReceiveResponseAsync()
    {
        var buffer = new byte[1024 * 4];
        try
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received response: {responseJson}");

                // Parse the response payload
                var response = JsonSerializer.Deserialize<ResponsePayload>(responseJson);

                // Decrypt the response message
                var decryptedMessage = EncryptionHelper.DecryptMessage(
                    Convert.FromBase64String(response.EncryptedMessage),
                    Convert.FromBase64String(response.AesKey),
                    Convert.FromBase64String(response.Iv)
                );

                Console.WriteLine($"Decrypted response: {decryptedMessage}");
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("Connection closed by Gateway Node.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving response: {ex.Message}");
        }
    }

    /// <summary>
    /// Represents the structure of the response payload.
    /// </summary>
    private class ResponsePayload
    {
        public string EncryptedMessage { get; set; }
        public string AesKey { get; set; }
        public string Iv { get; set; }
    }
}