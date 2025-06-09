using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace NetSocketHandler.Interface
{
    public interface IConnectionManager<TData>
    {
        void AddConnection(string connectionId, WebSocket socket, string identifier);
        void RemoveConnection(string connectionId);
        WebSocket GetConnectionByIdentifier(string identifier);
        WebSocket GetConnectionById(string connectionId);
        IEnumerable<WebSocket> GetAllConnections();
        Task SendDataToIdentifier(string identifier, TData data);
        bool IsIdentifierConnected(string identifier);
        Task SendDataAsync(TData data);
    }
}
