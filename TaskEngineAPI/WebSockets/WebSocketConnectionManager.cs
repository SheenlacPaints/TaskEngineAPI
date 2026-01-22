using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace TaskEngineAPI.WebSockets;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, List<WebSocketClient>> _connections = new();

    public void AddConnection(string tenantId, string userId, WebSocket socket)
    {
        var client = new WebSocketClient
        {
            UserId = userId,
            Socket = socket,
            ConnectedAt = DateTime.UtcNow
        };

        _connections.AddOrUpdate(
            tenantId,
            new List<WebSocketClient> { client },
            (key, list) => { list.Add(client); return list; }
        );
    }

    public void RemoveConnection(string tenantId, WebSocket socket)
    {
        if (_connections.TryGetValue(tenantId, out var clients))
        {
            clients.RemoveAll(c => c.Socket == socket);
            if (clients.Count == 0)
                _connections.TryRemove(tenantId, out _);
        }
    }

    public List<WebSocketClient> GetTenantConnections(string tenantId)
    {
        return _connections.TryGetValue(tenantId, out var clients)
            ? clients
            : new List<WebSocketClient>();
    }

    public int GetConnectionCount(string tenantId)
    {
        return _connections.TryGetValue(tenantId, out var clients)
            ? clients.Count
            : 0;
    }

    public Dictionary<string, int> GetAllConnectionCounts()
    {
        return _connections.ToDictionary(x => x.Key, x => x.Value.Count);
    }
}

public class WebSocketClient
{
    public string UserId { get; set; }
    public WebSocket Socket { get; set; }
    public DateTime ConnectedAt { get; set; }
}