using NetSocketHandler.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace NetSocketHandler.Implementation
{
    public class ConnectionManager<TData> : IConnectionManager<TData>
    {
        private readonly ConcurrentDictionary<string, WebSocket> _connections = new ConcurrentDictionary<string, WebSocket>();
        private readonly ConcurrentDictionary<string, string> _identifiers = new ConcurrentDictionary<string, string>();

        public void AddConnection(string connectionId, WebSocket socket, string identifier)
        {
            _connections.TryAdd(connectionId, socket);
            _identifiers.TryAdd(identifier, connectionId);
        }

        public void RemoveConnection(string connectionId)
        {

            if(connectionId is null)
            {
                return;
            }

            _connections.TryRemove(connectionId, out _);
            var identifier = _identifiers.FirstOrDefault(x => x.Value == connectionId).Key;
            if (!string.IsNullOrEmpty(identifier))
            {
                _identifiers.TryRemove(identifier, out _);
            }
        }

        public WebSocket GetConnectionByIdentifier(string identifier)
        {
            return _identifiers.TryGetValue(identifier, out var connectionId)
                ? GetConnectionById(connectionId)
                : null;
        }

        public WebSocket GetConnectionById(string connectionId)
        {
            return _connections.TryGetValue(connectionId, out var socket) ? socket : null;
        }

        public IEnumerable<WebSocket> GetAllConnections() => _connections.Values;

        public async Task SendDataToIdentifier(string identifier, TData data)
        {
            var socket = GetConnectionByIdentifier(identifier);
            if (socket != null && socket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(data.ToString());
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public bool IsIdentifierConnected(string identifier)
        {
            return GetConnectionByIdentifier(identifier) != null;
        }

        public async Task SendDataAsync(TData data)
        {
            var buffer = Encoding.UTF8.GetBytes(data.ToString());

            var tasks = GetAllConnections()
                .Where(socket => socket.State == WebSocketState.Open)
                .Select(socket => socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None));

            await Task.WhenAll(tasks);
        }
    }
}
