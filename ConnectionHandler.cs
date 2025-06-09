using NetSocketHandler.Implementation;
using NetSocketHandler.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetSocketHandler
{
    public class ConnectionHandler<TData> : IConnectionManager<TData>
    {
        private readonly ConnectionManager<TData> _connectionManager = new ConnectionManager<TData>();

        public async Task HandleConnectionAsync(WebSocket socket, string connectionId)
        {
            _connectionManager.AddConnection(connectionId, socket, connectionId);
            var buffer = new byte[1024 * 64];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var data = JsonSerializer.Deserialize<TData>(message);
                    await ProcessReceivedDataAsync(connectionId, data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                _connectionManager.RemoveConnection(connectionId);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                socket.Dispose();
            }
        }

        private async Task ProcessReceivedDataAsync(string connectionId, TData data)
        {
            Console.WriteLine($"Received from {connectionId}: {data}");
            await SendDataAsync(connectionId, data);
        }

        public async Task SendDataAsync(string connectionId, TData data)
        {
            var socket = _connectionManager.GetConnectionById(connectionId);
            if (socket != null && socket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(data.ToString());
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        // Delegate methods to the ConnectionManager
        public void AddConnection(string connectionId, WebSocket socket, string identifier) => _connectionManager.AddConnection(connectionId, socket, identifier);
        public void RemoveConnection(string connectionId) => _connectionManager.RemoveConnection(connectionId);
        public WebSocket GetConnectionByIdentifier(string identifier) => _connectionManager.GetConnectionByIdentifier(identifier);
        public WebSocket GetConnectionById(string connectionId) => _connectionManager.GetConnectionById(connectionId);
        public IEnumerable<WebSocket> GetAllConnections() => _connectionManager.GetAllConnections();
        public Task SendDataToIdentifier(string identifier, TData data) => _connectionManager.SendDataToIdentifier(identifier, data);
        public bool IsIdentifierConnected(string identifier) => _connectionManager.IsIdentifierConnected(identifier);
        public Task SendDataAsync(TData data) => _connectionManager.SendDataAsync(data);
    }
}
