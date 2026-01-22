using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Services;
using TaskEngineAPI.Models;
public class ProjectSocketHandler
{
    private readonly IServiceProvider _serviceProvider;

    public ProjectSocketHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var socket = await context.WebSockets.AcceptWebSocketAsync();

        var buffer = new byte[1024 * 4];

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                CancellationToken.None
            );

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var requestJson = Encoding.UTF8.GetString(buffer, 0, result.Count);

            var request = JsonConvert.DeserializeObject<SocketRequest>(requestJson);

            // 🔥 Call your existing service
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TaskMasterService>();

            var data = await service.Getworkflowdashboard(
                request.cTenantID,
                request.username,            
                request.searchText
            );
            var responseBytes = Encoding.UTF8.GetBytes(data);

            await socket.SendAsync(
                new ArraySegment<byte>(responseBytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        await socket.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Closed",
            CancellationToken.None
        );
    }
}
