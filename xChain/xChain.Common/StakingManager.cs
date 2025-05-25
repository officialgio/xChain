namespace xChain.Common;

/// <summary>
/// Manages staking and slashing for Service Nodes.
/// </summary>
public class StakingManager
{
    private readonly Dictionary<string, decimal> _stakes = new();

    /// <summary>
    /// Adds a stake for a Service Node.
    /// </summary>
    public void AddStake(string nodeId, decimal amount)
    {
        if (_stakes.ContainsKey(nodeId))
            _stakes[nodeId] += amount;
        else
            _stakes[nodeId] = amount;
    }

    /// <summary>
    /// Reduces a stake (slashing) for a Service Node.
    /// </summary>
    public void SlashStake(string nodeId, decimal amount)
    {
        if (_stakes.ContainsKey(nodeId))
        {
            _stakes[nodeId] -= amount;
            if (_stakes[nodeId] <= 0)
            {
                _stakes.Remove(nodeId);
                Console.WriteLine($"Node {nodeId} has been removed due to insufficient stake.");
            }
        }
    }

    /// <summary>
    /// Gets the current stake for a Service Node.
    /// </summary>
    public decimal GetStake(string nodeId)
    {
        return _stakes.TryGetValue(nodeId, out var stake) ? stake : 0;
    }
}