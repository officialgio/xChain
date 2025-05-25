Console.WriteLine("Starting Service Node...");

const string relayServiceEndpoint = "http://localhost:5000";

// Create and start the service node
var node = new ServiceNode.ServiceNode("Node1", relayServiceEndpoint);
await node.StartAsync("http://localhost:5001");