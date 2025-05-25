using System.Security.Cryptography;
using System.Text;

namespace xChain.Common;

/// <summary>
/// Implements a Rendezvous Hashing (consistent hashing) mechanism.
/// </summary>
public class HashRing<T>
{
    private readonly SortedDictionary<int, T> _ring = new();
    private readonly int _replicas;

    public HashRing(int replicas = 100)
    {
        _replicas = replicas;
    }

    /// <summary>
    /// Adds a node to the hash ring.
    /// </summary>
    public void AddNode(T node)
    {
        for (int i = 0; i < _replicas; i++)
        {
            int hash = GetHash(node.ToString() + i);
            _ring[hash] = node;
        }
    }

    /// <summary>
    /// Removes a node from the hash ring.
    /// </summary>
    public void RemoveNode(T node)
    {
        for (int i = 0; i < _replicas; i++)
        {
            int hash = GetHash(node.ToString() + i);
            _ring.Remove(hash);
        }
    }

    /// <summary>
    /// Gets the node responsible for the given key.
    /// </summary>
    public T GetNode(string key)
    {
        if (!_ring.Any())
            throw new InvalidOperationException("No nodes in the hash ring.");

        int hash = GetHash(key);
        foreach (var nodeHash in _ring.Keys)
        {
            if (hash <= nodeHash)
                return _ring[nodeHash];
        }

        return _ring[_ring.Keys.First()];
    }

    public int GetNodeCount() => _ring.Count;

    private int GetHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToInt32(hash, 0);
    }
}