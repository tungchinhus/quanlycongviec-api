using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace quanlyfilesBE.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var nameIdentifier = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var sub = Context.User?.FindFirst("sub")?.Value;
        var firebaseUid = Context.User?.FindFirst("firebase_uid")?.Value;
        var userId = nameIdentifier ?? sub ?? firebaseUid;
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Join group với userId để chỉ nhận notifications của chính user đó
            var groupName = $"user_{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}

