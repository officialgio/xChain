using xChain.Wallet;

Console.WriteLine("Starting Wallet Client...");

// Create and start the Wallet Client
var wallet = new WalletClient("ws://localhost:5002");
await wallet.ConnectAsync();

// Send a test message
await wallet.SendMessageAsync("Hello, Astra Network!");

// Keep the client running
Console.ReadLine();