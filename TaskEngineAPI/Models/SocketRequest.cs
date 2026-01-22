using System.Net.WebSockets;

namespace TaskEngineAPI.Models
{
    
    public class SocketRequest
    {
        public int cTenantID { get; set; }
        public string username { get; set; }
        public string? type { get; set; }
        public string? searchText { get; set; }
    }
    public class WebSocketClient
    {
        public string UserId { get; set; }
        public WebSocket Socket { get; set; }
        public DateTime ConnectedAt { get; set; }
    }

    public class WebSocketRequest
    {
        public string Action { get; set; }
    }

}
