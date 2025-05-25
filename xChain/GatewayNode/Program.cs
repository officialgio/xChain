Console.WriteLine("Starting Gateway Node...");

// Create and start the Gateway Node
var gatewayNode = new GatewayNode.GatewayNode("http://localhost:5000");
await gatewayNode.StartAsync("http://localhost:5002");