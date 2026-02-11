using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using TaskEngineAPI.Services;
using Minio.Exceptions;
using Azure;

namespace TaskEngineAPI.WebSockets;

public class ProjectSocketHandler
{
    private readonly ILogger<ProjectSocketHandler> _logger;
    private readonly WebSocketConnectionManager _connectionManager;
    private readonly Interfaces.IWorkflowService _workflowService;

    public ProjectSocketHandler(
        ILogger<ProjectSocketHandler> logger,
        WebSocketConnectionManager connectionManager,
        Interfaces.IWorkflowService workflowService)
    {
        _logger = logger;
        _connectionManager = connectionManager;
        _workflowService = workflowService;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        var tenantId = context.User.FindFirst("cTenantID")?.Value
                       ?? context.User.FindFirst("cTenantID")?.Value;
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("username")?.Value;

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Missing tenant or user information");
            return;
        }

        if (_connectionManager.GetConnectionCount(tenantId) >= 100)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Connection limit reached for tenant");
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        _connectionManager.AddConnection(tenantId, userId, webSocket);

        _logger.LogInformation("WebSocket connected - Tenant: {TenantId}, User: {UserId}", tenantId, userId);

        try
        {
            await ReceiveMessagesAsync(webSocket, tenantId, userId);
        }
        finally
        {
            _connectionManager.RemoveConnection(tenantId, webSocket);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            _logger.LogInformation("WebSocket disconnected - Tenant: {TenantId}, User: {UserId}", tenantId, userId);
        }
    }

    //private async Task ReceiveMessagesAsync(WebSocket webSocket, string tenantId, string userId)
    //{
    //    var buffer = new byte[1024 * 4];

    //    while (webSocket.State == WebSocketState.Open)
    //    {
    //        try
    //        {
    //            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

    //            if (result.MessageType == WebSocketMessageType.Close)
    //            {
    //                _logger.LogInformation("Client requested disconnect.");
    //                break; // Exit loop, which triggers the 'finally' close logic
    //            }
    //           // break;

    //            if (result.MessageType == WebSocketMessageType.Text)
    //            {
    //                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
    //                _logger.LogInformation("Received message from Tenant: {TenantId}, User: {UserId}, Message: {Message}",
    //                    tenantId, userId, message);

    //                var response = await ProcessMessageAsync(message, tenantId, userId);
    //                await SendMessageAsync(webSocket, response);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError(ex, "Error processing message for tenant {TenantId}", tenantId);
    //            await SendMessageAsync(webSocket, JsonSerializer.Serialize(new { error = "Internal error" }));
    //        }
    //    }
    //}

    private async Task ReceiveMessagesAsync(WebSocket webSocket, string tenantId, string userId)
    {
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            try
            {
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage); 
                if (result.MessageType == WebSocketMessageType.Close) break;
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms, Encoding.UTF8);
                var message = await reader.ReadToEndAsync();
                var response = await ProcessMessageAsync(message, tenantId, userId);
                await SendMessageAsync(webSocket, response);
            }
            //catch { break; }
            catch (Exception ex)
            {
                try
                {
                    var errorResponse = JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = "Processing error",
                        message = ex.Message,  // Include error details
                        timestamp = DateTime.UtcNow
                    });
                    await SendMessageAsync(webSocket, errorResponse);
                }
                catch
                {
                    break; // Only break if send fails
                }

            }

           

        }
    }



    private async Task<string> ProcessMessageAsync(string message, string tenantId, string userId)
    {
        try
        {
            var request = JsonSerializer.Deserialize<WebSocketRequest>(message);

            switch (request?.Action?.ToLower())
            {
                case "getworkflowdashboard":
                    var data = await _workflowService.GetWorkflowDashboardAsync(tenantId, userId);
                    return JsonSerializer.Serialize(new
                    {
                        type = "workflowDashboard",
                        success = true,
                        data = data,
                        timestamp = DateTime.UtcNow
                    });

                case "ping":
                    return JsonSerializer.Serialize(new
                    {
                        type = "pong",
                        timestamp = DateTime.UtcNow
                    });

                default:
                    return JsonSerializer.Serialize(new
                    {
                        success = false,
                        error = "Unknown action",
                        availableActions = new[] { "getWorkflowDashboard", "ping" }
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return JsonSerializer.Serialize(new { success = false, error = "Processing error" });
        }
    }

    //private async Task SendMessageAsync(WebSocket webSocket, string message)
    //{
    //    if (webSocket.State == WebSocketState.Open)
    //    {
    //        var bytes = Encoding.UTF8.GetBytes(message);
    //        await webSocket.SendAsync(
    //            new ArraySegment<byte>(bytes),
    //            WebSocketMessageType.Text,
    //            true,
    //            CancellationToken.None);
    //    }
    //}
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
    private async Task SendMessageAsync(WebSocket webSocket, string message)
    {
        if (webSocket == null || webSocket.State != WebSocketState.Open) return;

        await _sendLock.WaitAsync(); 
        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send failed - connection may have dropped.");
        }
        finally
        {
            _sendLock.Release(); 
        }
    }


    public async Task BroadcastToTenantAsync(string tenantId, object data)
    {
        var message = JsonSerializer.Serialize(data);
        var clients = _connectionManager.GetTenantConnections(tenantId);

        var tasks = clients
            .Where(c => c.Socket.State == WebSocketState.Open)
            .Select(c => SendMessageAsync(c.Socket, message));

        await Task.WhenAll(tasks);
    }
}

public class WebSocketRequest
{
    public string Action { get; set; }
}