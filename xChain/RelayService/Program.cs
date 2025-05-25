Console.WriteLine("Starting Relay Service...");

// Create and start the Relay Service
var relayService = new RelayService.RelayService();
await relayService.StartAsync("http://localhost:5000");